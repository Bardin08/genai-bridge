using GenAI.Bridge.Contracts.Prompts;
using PublicApiGenerator;

public class PublicApiApprovalTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var assembly = typeof(CompletionPrompt).Assembly;
        var publicApi = assembly.GeneratePublicApi();
        return Verify(publicApi);
    }
}