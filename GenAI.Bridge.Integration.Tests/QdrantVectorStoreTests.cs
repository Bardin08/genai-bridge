using Xunit;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GenAI.Bridge.Adapters;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class QdrantVectorStoreTests
{
    private static readonly string QdrantUrl =
        Environment.GetEnvironmentVariable("QDRANT_URL") ?? "http://localhost:6333";
    private static readonly string TestCollection =
        Environment.GetEnvironmentVariable("QDRANT_COLLECTION") ?? "genai_test" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task CanUpsertAndSearch()
    {
        var qdrantClient = new QdrantClient(host: "localhost", port: 6334, https: false);

        await qdrantClient.CreateCollectionAsync(TestCollection, new VectorParams
        {
            Size = 4,
            Distance = Distance.Cosine
        });

        var store = new QdrantVectorStore(qdrantClient, TestCollection);

        var vector1Id = Guid.NewGuid();
        var vector2Id = Guid.NewGuid();
        
        await store.UpsertAsync(vector1Id, [1, 0, 0, 0], []);
        await store.UpsertAsync(vector2Id, [0, 1, 0, 0], []);

        var hits = await store.SearchAsync([1, 0, 0, 0], topK: 2);
        Assert.Contains(hits, h => h.ExternalId == vector1Id);
        Assert.Equal(2, hits.Count);
        Assert.True(hits[0].Score >= hits[1].Score);
    }
}