using System.Reflection;

namespace GenAI.Bridge.Utils;

/// <summary>
/// Utility class for resolving types by name and generating schemas from them
/// </summary>
public static class TypeResolver
{
    private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the TypeResolver by discovering types from loaded assemblies
    /// </summary>
    static TypeResolver()
    {
        DiscoverTypes();
    }

    /// <summary>
    /// Generates a JSON schema from a type name string
    /// </summary>
    /// <param name="typeName">The name of the type to generate a schema for</param>
    /// <returns>A JSON schema string, or null if the type cannot be found</returns>
    public static string? GenerateSchemaFromTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var type = ResolveType(typeName);
        if (type == null)
            return null;

        try
        {
            return OpenAiJsonSchemaUtils.GenerateSchema(type, type.Name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a type by name
    /// </summary>
    /// <param name="typeName">The name of the type to resolve</param>
    /// <returns>The resolved Type, or null if not found</returns>
    public static Type? ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        // Check cache first
        if (TypeCache.TryGetValue(typeName, out var cachedType))
            return cachedType;

        // Try to resolve the type directly if it's a fully qualified name
        var type = Type.GetType(typeName);
        if (type != null)
        {
            TypeCache[typeName] = type;
            return type;
        }

        // If the type wasn't found directly, search through known types in the cache
        foreach (var knownType in TypeCache.Values)
        {
            if (!knownType.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                knownType.FullName?.EndsWith($".{typeName}", StringComparison.OrdinalIgnoreCase) != true) continue;

            TypeCache[typeName] = knownType;
            return knownType;
        }

        return null;
    }

    /// <summary>
    /// Discovers types from loaded assemblies and populates the type cache
    /// </summary>
    private static void DiscoverTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                // Skip system assemblies to save time and memory
                if (assembly.IsDynamic || IsSystemAssembly(assembly))
                    continue;

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (!type.IsPublic ||
                        type is not { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
                        continue;

                    if (!TypeCache.ContainsKey(type.FullName!))
                    {
                        TypeCache[type.FullName!] = type;
                    }

                    TypeCache.TryAdd(type.Name, type);
                }
            }
            catch
            {
                // Ignore assemblies that can't be loaded or processed
            }
        }
    }

    /// <summary>
    /// Refreshes the type cache by rediscovering types
    /// </summary>
    public static void RefreshTypeCache()
    {
        TypeCache.Clear();
        DiscoverTypes();
    }

    /// <summary>
    /// Checks if an assembly is a system assembly
    /// </summary>
    private static bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return name != null && (
            name.StartsWith("System.") ||
            name.StartsWith("Microsoft.") ||
            name == "System" ||
            name == "mscorlib"
        );
    }
}
