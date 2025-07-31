namespace GenAI.Bridge.Context;

internal static class ContextKeysBuilder
{
    public static string InputKey(
        string stageId,
        string parameterName) => $"stage:{stageId}:input:{parameterName}";

    public static string InputParamsKey(
        string stageId,
        string parameterName) => $"stage:{stageId}:input:params:{parameterName}";

    public static string ToolCallKey(
        string stageId,
        string toolName,
        string callId) => $"stage:{stageId}:tool:{toolName}:{callId}";

    public static string MetadataKey(
        string stageId,
        string key) => $"stage:{stageId}:metadata:{key}";

    public static string OutputKey(string stageId) => $"stage:{stageId}:output";

    public static string OutputParamKey(
        string stageId,
        string parameterName) => $"stage:{stageId}:output:params:{parameterName}";

    public static string OutputLogKey(
        string stageId,
        string logType) => $"stage:{stageId}:output:{logType}";
}