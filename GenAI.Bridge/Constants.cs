using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenAI.Bridge;

internal static class Constants
{
    internal static class Roles
    {
        internal const string System = "system";
        internal const string User = "user";
        internal const string Assistant = "assistant";
    }

    internal static class Json
    {
        internal static JsonSerializerOptions DefaultSettings = new()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: true)
            },
            WriteIndented = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
}