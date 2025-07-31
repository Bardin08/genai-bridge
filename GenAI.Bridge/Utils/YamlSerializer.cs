using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAI.Bridge.Utils;

internal static class YamlSerializer
{
    internal static string SerializeToYaml(this object obj)
        => new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Serialize(obj);

    internal static T DeserializeFromYaml<T>(this string yaml)
        => new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Deserialize<T>(yaml);
}