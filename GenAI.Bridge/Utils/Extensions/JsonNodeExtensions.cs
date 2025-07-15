using System.Text;
using System.Text.Json.Nodes;

namespace GenAI.Bridge.Utils.Extensions;

internal static class JsonNodeExtensions
{
    /// <summary>
    /// Debug method to inspect the JSON structure.
    /// </summary>
    /// <param name="rootNode">JSON node to visualize</param>
    /// <param name="maxProperties">Maximum properties to show</param>
    /// <returns>Debug information about the JSON structure</returns>
    public static string VisualizeJson(JsonNode rootNode, int maxProperties = 10)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"JSON Navigator Debug - Root Type: {rootNode?.GetType().Name}");
        
        switch (rootNode)
        {
            case JsonObject rootObject:
            {
                sb.AppendLine($"Root Object Properties ({rootObject.Count} total):");
                var count = 0;
                foreach (var property in rootObject)
                {
                    if (count >= maxProperties)
                    {
                        sb.AppendLine($"... and {rootObject.Count - maxProperties} more properties");
                        break;
                    }
                
                    var valueType = property.Value?.GetType().Name ?? "null";
                    var valuePreview = GetValuePreview(property.Value);
                    sb.AppendLine($"  - {property.Key}: {valueType} {valuePreview}");
                    count++;
                }

                break;
            }
            case JsonArray rootArray:
            {
                sb.AppendLine($"Root Array Length: {rootArray.Count}");
                if (rootArray.Count > 0)
                {
                    var firstElementType = rootArray[0]?.GetType().Name ?? "null";
                    sb.AppendLine($"First Element Type: {firstElementType}");
                }

                break;
            }
            default:
                sb.AppendLine($"Root Value: {rootNode}");
                break;
        }
        
        return sb.ToString();
    }

    private static string GetValuePreview(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => $"({obj.Count} properties)",
            JsonArray arr => $"[{arr.Count} items]",
            _ => $"= {node?.ToString()?[..Math.Min(50, node.ToString()?.Length ?? 0)] ?? "null"}"
        };
    }
}