using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAI.Bridge.Abstractions;
using GenAI.Bridge.Contracts.Configuration;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Results;
using GenAI.Bridge.Tooling;
using GenAI.Bridge.Utils.Extensions;
using OpenAI;
using OpenAI.Chat;

namespace GenAI.Bridge.Providers.OpenAI;

public static class MetadataKeys
{
    public const string Id = nameof(Id);
    public const string Model = nameof(Model);
    public const string FinishReason = nameof(FinishReason);
    public const string ToolCalls = nameof(ToolCalls);
    public const string InputTokens = nameof(InputTokens);
    public const string OutputTokens = nameof(OutputTokens);
    public const string TotalTokens = nameof(TotalTokens);
}

/// <summary>
/// OpenAI implementation of the LLM adapter interface that relies on an
/// external <see cref="IFunctionRegistry"/> for function execution and uses
/// <see cref="MetadataKeys"/> to populate response metadata in a uniform way.
/// </summary>
public sealed class OpenAiLlmAdapter : ILlmAdapter
{
    private readonly OpenAiLlmAdapterConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IReadOnlyDictionary<string, ChatClient> _chatClients;
    private readonly IFunctionRegistry _registry;

    public OpenAiLlmAdapter(
        OpenAiLlmAdapterConfig config,
        IFunctionRegistry registry,
        IReadOnlyDictionary<string, ChatClient>? chatClients = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _chatClients = chatClients ?? CreateChatClients(_config);
        _config.Validate();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public IReadOnlySet<string> SupportedModels => _config.SupportedModels;

    public async Task<CompletionResult> CompleteAsync(string model,
        CompletionPrompt prompt,
        CancellationToken ct = default)
    {
        var messages = BuildInitialMessages(prompt);
        var options = BuildOptions(prompt);
        var response = await RunConversationAsync(model, messages, options, ct);

        var promptMetadata = prompt.UserPromptTurn.Parameters?
            .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase) ?? [];

        var fullMetadata = promptMetadata.Merge(response.metadata);
        return new CompletionResult(prompt.SessionId, response.response, prompt.SystemMessage, prompt.UserPromptTurn,
            fullMetadata.AsReadOnly());
    }

    private static Dictionary<string, ChatClient> CreateChatClients(OpenAiLlmAdapterConfig cfg)
    {
        var clients = new Dictionary<string, ChatClient>(StringComparer.OrdinalIgnoreCase);
        foreach (var model in cfg.SupportedModels.Where(model => !clients.ContainsKey(model)))
        {
            clients[model] = new ChatClient(
                model,
                new ApiKeyCredential(cfg.ApiKey),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds),
                    OrganizationId = cfg.OrganizationId,
                    ProjectId = cfg.ProjectId,
                    RetryPolicy = new ClientRetryPolicy(maxRetries: 5)
                });
        }

        return clients;
    }

    private static List<ChatMessage> BuildInitialMessages(CompletionPrompt prompt)
    {
        var list = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(prompt.SystemMessage))
            list.Add(ChatMessage.CreateSystemMessage(prompt.SystemMessage));

        if (!prompt.UserPromptTurn.Role.Equals(Constants.Roles.User, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Unsupported role '{prompt.UserPromptTurn.Role}'. Expected 'user'.");

        list.Add(ChatMessage.CreateUserMessage(prompt.UserPromptTurn.Content));
        return list;
    }

    private ChatCompletionOptions BuildOptions(CompletionPrompt prompt)
    {
        var p = prompt.UserPromptTurn;
        var o = new ChatCompletionOptions
        {
            MaxOutputTokenCount = p.MaxTokens.GetValueOrDefault(4096),
            Temperature = p.Temperature.GetValueOrDefault(1.0f),
            TopP = p.TopP.GetValueOrDefault(1.0f)
        };

        // TODO: OpenAI deprecated function calls in favor of tools.
        // Now it's still possible to use them, but in the meanwhile migration to tools is recommended.
        if (p.Functions is { } fnCfg)
        {
            foreach (var f in fnCfg.Functions)
                o.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: f.Name,
                    functionDescription: f.Description,
                    functionParameters: BinaryData.FromString(f.Parameters.ToString()!),
                    functionSchemaIsStrict: true));
        }

        if (p.Tools is { } tCfg)
        {
            foreach (var t in tCfg)
                o.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: t.Function.Name,
                    functionDescription: t.Function.Description,
                    functionParameters: BinaryData.FromString(t.Function.Parameters.ToString()!),
                    functionSchemaIsStrict: true));

            o.ToolChoice = p.Functions?.FunctionCall?.Name switch
            {
                "auto" => ChatToolChoice.CreateAutoChoice(),
                "none" => ChatToolChoice.CreateNoneChoice(),
                var name when !string.IsNullOrWhiteSpace(name) => ChatToolChoice.CreateFunctionChoice(name),
                _ => ChatToolChoice.CreateNoneChoice()
            };

            o.AllowParallelToolCalls = _config.AllowParallelToolCalls;
        }

        o.ResponseFormat = p.ResponseFormat?.Type switch
        {
            ResponseFormatType.Text => ChatResponseFormat.CreateTextFormat(),
            ResponseFormatType.JsonObject => ChatResponseFormat.CreateJsonObjectFormat(),
            ResponseFormatType.JsonSchema when !string.IsNullOrWhiteSpace(p.ResponseFormat.Schema)
                => ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: p.Name,
                    jsonSchema: BinaryData.FromString(p.ResponseFormat.Schema),
                    jsonSchemaIsStrict: true),
            _ => throw new ArgumentException("Unsupported or missing response format.")
        };
        return o;
    }

    private async Task<(string response, Dictionary<string, object> metadata)> RunConversationAsync(
        string model,
        List<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken ct)
    {
        var client = _chatClients[model];
        var auditList = new List<object>();

        while (true)
        {
            var resp = await client.CompleteChatAsync(messages, options, ct);
            var text = resp.Value.Content.FirstOrDefault()?.Text;
            var calls = resp.Value.ToolCalls?.ToList() ?? [];

            // finished
            if (calls.Count == 0)
            {
                var meta = new Dictionary<string, object>
                {
                    [MetadataKeys.Id] = resp.Value.Id,
                    [MetadataKeys.Model] = resp.Value.Model,
                    [MetadataKeys.FinishReason] = resp.Value.FinishReason.ToString(),
                    [MetadataKeys.ToolCalls] = auditList
                };

                if (resp.Value.Usage is not { } u)
                    return (text ?? string.Empty, meta);

                meta[MetadataKeys.InputTokens] = u.InputTokenCount;
                meta[MetadataKeys.OutputTokens] = u.OutputTokenCount;
                meta[MetadataKeys.TotalTokens] = u.TotalTokenCount;
                return (text ?? string.Empty, meta);
            }

            // execute tool calls
            var toolMsgs = await Task.WhenAll(calls.Select(async toolCall =>
            {
                var args = JsonSerializer.Deserialize<JsonElement>(toolCall.FunctionArguments, _jsonOptions);
                if (!_registry.TryGet(toolCall.FunctionName, out var toolDelegate))
                    throw new InvalidOperationException($"Unknown function '{toolCall.FunctionName}'.");

                var resultJson = await Task.Run(() => toolDelegate(args), ct);

                auditList.Add(new ToolCallAudit
                {
                    Id = toolCall.Id,
                    FunctionName = toolCall.FunctionName,
                    Arguments = args,
                    Result = resultJson
                });
                return ChatMessage.CreateToolMessage(toolCall.Id, resultJson);
            }));

            messages.AddRange(toolMsgs);
        }
    }
}

public record ToolCallAudit
{
    public required string Id { get; init; }
    public required string FunctionName { get; init; }
    public JsonElement Arguments { get; init; }
    public required object Result { get; init; }

    public override string ToString()
        => $"ToolCallAudit(Id: {Id}, FunctionName: {FunctionName}, Arguments: {Arguments}, Result: {Result})";
}