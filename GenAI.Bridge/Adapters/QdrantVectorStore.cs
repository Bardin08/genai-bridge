using GenAI.Bridge.Abstractions;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace GenAI.Bridge.Adapters;

public sealed class QdrantVectorStore(QdrantClient qClient, string collectionName) : IVectorStore
{
    public async Task UpsertAsync(Guid externalId, IReadOnlyList<float> vector,
        Dictionary<string, string> payload, CancellationToken ct = default)
    {
        var points = GetPointsList(externalId, vector, payload);

        var result = await qClient.UpsertAsync(collectionName, points, wait: true, cancellationToken: ct);

        if (result.Status is not UpdateStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Qdrant upsert completed with non-success status: {result.Status.ToString()}");
        }
    }

    public async Task<IReadOnlyList<(Guid ExternalId, float Score, IReadOnlyList<float> Vector)>> SearchAsync(
        IReadOnlyList<float> queryVector, uint topK, CancellationToken ct = default)
    {
        var result = await qClient.SearchAsync(collectionName,
            vector: queryVector.ToArray(),
            limit: topK,
            vectorsSelector: true, 
            cancellationToken: ct);

        return result
            .Select(x => (
                Id: Guid.Parse(x.Id.Uuid),
                Score: x.Score,
                Vector: x.Vectors.Vector.Data.ToList() as IReadOnlyList<float>))
            .ToList();
    }

    private static List<PointStruct> GetPointsList(
        Guid externalId, IReadOnlyList<float> vector, Dictionary<string, string> payload)
    {
        var point = new PointStruct
        {
            Id = externalId,
            Vectors = vector.ToArray()
        };

        foreach (var p in payload)
        {
            point.Payload[p.Key] = p.Value;
        }

        return [point];
    }
}