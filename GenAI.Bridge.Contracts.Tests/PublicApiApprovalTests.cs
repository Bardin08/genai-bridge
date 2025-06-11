using PublicApiGenerator;

public class PublicApiApprovalTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var assembly = typeof(GenAI.Bridge.Contracts.CompletionPrompt).Assembly;
        var publicApi = assembly.GeneratePublicApi();
        return Verify(publicApi);
    }
}