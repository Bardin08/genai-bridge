using System.Text.Json;
using System.Text.Json.Nodes;

namespace GenAI.Bridge.Utils;

/// <summary>
/// Provides functionality to navigate through JSON structures using path-based access.
/// </summary>
internal class JsonNavigator
{
    private const int MaxPropertiesPerObject = 100;
    private const int MaxArrayElementsTraversal = 50;
    private const int MaxKeysInErrorMessage = 10;
    private const int DefaultMaxPathsPerLevel = 100;
    
    private readonly JsonNode _rootNode;
    private readonly char _pathSeparator;

    /// <summary>
    /// Initializes a new instance of JsonNavigator with a JSON string.
    /// </summary>
    /// <param name="jsonString">The JSON string to parse</param>
    /// <param name="pathSeparator">The character used to separate path segments (default: ':')</param>
    /// <exception cref="ArgumentException">Thrown when JSON string is invalid</exception>
    internal JsonNavigator(string jsonString, char pathSeparator = ':')
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonString));

        try
        {
            _rootNode = JsonNode.Parse(jsonString) ?? throw new ArgumentException("JSON string resulted in null node");
            _pathSeparator = pathSeparator;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON string: {ex.Message}", nameof(jsonString), ex);
        }
    }

    /// <summary>
    /// Initializes a new instance of JsonNavigator with a JsonNode.
    /// </summary>
    /// <param name="jsonNode">The root JsonNode</param>
    /// <param name="pathSeparator">The character used to separate path segments (default: ':')</param>
    internal JsonNavigator(JsonNode jsonNode, char pathSeparator = ':')
    {
        _rootNode = jsonNode ?? throw new ArgumentNullException(nameof(jsonNode));
        _pathSeparator = pathSeparator;
    }

    
    /// <summary>
    /// Gets the value at the specified path as a string.
    /// </summary>
    /// <param name="path">The path to the value</param>
    /// <returns>The string representation of the value, or null if not found</returns>
    public string? GetValue(string path)
    {
        var result = Navigate(path);
        return result.IsSuccess ? result.Node?.ToString() : null;
    }

    
    /// <summary>
    /// Gets the value at the specified path as a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to</typeparam>
    /// <param name="path">The path to the value</param>
    /// <returns>The converted value, or default(T) if not found or conversion fails</returns>
    public T? GetValue<T>(string path)
    {
        var result = Navigate(path);
        if (!result.IsSuccess || result.Node == null)
            return default(T);

        try
        {
            return result.Node.GetValue<T>();
        }
        catch
        {
            return default(T);
        }
    }

    /// <summary>
    /// Attempts to get the value at the specified path.
    /// </summary>
    /// <param name="path">The path to the value</param>
    /// <param name="value">The retrieved value as string</param>
    /// <returns>True if the value was found, false otherwise</returns>
    public bool TryGetValue(string path, out string? value)
    {
        var result = Navigate(path);
        if (result.IsSuccess)
        {
            value = result.Node?.ToString();
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get the value at the specified path as a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to</typeparam>
    /// <param name="path">The path to the value</param>
    /// <param name="value">The retrieved and converted value</param>
    /// <returns>True if the value was found and converted successfully, false otherwise</returns>
    public bool TryGetValue<T>(string path, out T? value)
    {
        var result = Navigate(path);
        if (result.IsSuccess && result.Node != null)
        {
            try
            {
                value = result.Node.GetValue<T>();
                return true;
            }
            catch
            {
                // Conversion failed
            }
        }

        value = default(T);
        return false;
    }

    /// <summary>
    /// Checks if a path exists in the JSON structure.
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path exists, false otherwise</returns>
    public bool PathExists(string path)
    {
        return Navigate(path).IsSuccess;
    }

    /// <summary>
    /// Gets all available paths in the JSON structure with early exit for deep structures.
    /// </summary>
    /// <param name="maxDepth">Maximum depth to traverse (default: 5)</param>
    /// <param name="maxPathsPerLevel">Maximum paths to return per level (default: 100)</param>
    /// <returns>Collection of available paths</returns>
    public IEnumerable<string> GetAllPaths(int maxDepth = 5, int maxPathsPerLevel = DefaultMaxPathsPerLevel)
    {
        var pathCount = new PathCounter { Count = 0 };
        return GetAllPathsRecursive(_rootNode, string.Empty, maxDepth, pathCount, maxPathsPerLevel);
    }

    /// <summary>
    /// Navigates to a specific path in the JSON structure.
    /// </summary>
    /// <param name="path">The path to navigate (e.g., "users:0:name")</param>
    /// <returns>NavigationResult containing the found node or error information</returns>
    private NavigationResult Navigate(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                return NavigationResult.Success(_rootNode, path);

            var pathParts = path.Split(_pathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var currentNode = _rootNode;
            var traversedPath = new List<string>();

            foreach (var part in pathParts)
            {
                if (currentNode == null)
                    return NavigationResult.Failure($"Node is null at path: {string.Join(_pathSeparator, traversedPath)}");

                if (int.TryParse(part, out var index))
                {
                    // Array access
                    if (currentNode is not JsonArray jsonArray)
                    {
                        return NavigationResult.Failure(
                            $"Attempted array access on non-array node at path: {string.Join(_pathSeparator, traversedPath)}");
                    }

                    if (index < 0 || index >= jsonArray.Count)
                    {
                        return NavigationResult.Failure(
                            $"Array index {index} out of bounds (length: {jsonArray.Count}) at path: {string.Join(_pathSeparator, traversedPath)}");
                    }

                    currentNode = jsonArray[index];
                    traversedPath.Add(part);
                }
                else
                {
                    // Object property access
                    if (currentNode is not JsonObject jsonObject)
                    {
                        return NavigationResult.Failure(
                            $"Attempted property access on non-object node at path: {string.Join(_pathSeparator, traversedPath)}");
                    }

                    if (!jsonObject.TryGetPropertyValue(part, out var propertyValue))
                    {
                        var availableKeys = GetAvailableKeys(jsonObject);
                        return NavigationResult.Failure(
                            $"Property '{part}' not found at path: {string.Join(_pathSeparator, traversedPath)}. Available keys: [{availableKeys}]");
                    }

                    currentNode = propertyValue;
                    traversedPath.Add(part);
                }
            }

            return NavigationResult.Success(currentNode, path);
        }
        catch (Exception ex)
        {
            return NavigationResult.Failure($"Unexpected error during navigation: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets available keys from a JsonObject, with early exit for large objects.
    /// </summary>
    private static string GetAvailableKeys(JsonObject jsonObject, int maxKeys = MaxKeysInErrorMessage)
    {
        var keys = jsonObject.Select(kv => kv.Key).Take(maxKeys).ToList();
        var result = string.Join(", ", keys);
        
        if (jsonObject.Count > maxKeys)
        {
            result += $" ... ({jsonObject.Count - maxKeys} more)";
        }
        
        return result;
    }

    private IEnumerable<string> GetAllPathsRecursive(
        JsonNode? node, string currentPath, int remainingDepth, PathCounter pathCount, int maxPathsPerLevel)
    {
        if (node == null || remainingDepth <= 0 || pathCount.Count >= maxPathsPerLevel)
            yield break;

        // Return current path if not empty
        if (!string.IsNullOrEmpty(currentPath))
        {
            yield return currentPath;
            pathCount.Count++;
            
            if (pathCount.Count >= maxPathsPerLevel)
                yield break;
        }

        switch (node)
        {
            case JsonObject jsonObject:
                var propertyCount = 0;
                foreach (var property in jsonObject)
                {
                    if (propertyCount >= MaxPropertiesPerObject || pathCount.Count >= maxPathsPerLevel)
                        break;
                        
                    var newPath = string.IsNullOrEmpty(currentPath) 
                        ? property.Key 
                        : $"{currentPath}{_pathSeparator}{property.Key}";
                    
                    foreach (var path in GetAllPathsRecursive(property.Value, newPath, remainingDepth - 1, pathCount, maxPathsPerLevel))
                    {
                        yield return path;
                        if (pathCount.Count >= maxPathsPerLevel)
                            yield break;
                    }
                    
                    propertyCount++;
                }
                break;

            case JsonArray jsonArray:
                var arrayLength = Math.Min(jsonArray.Count, MaxArrayElementsTraversal);
                for (var i = 0; i < arrayLength; i++)
                {
                    if (pathCount.Count >= maxPathsPerLevel)
                        break;
                        
                    var newPath = string.IsNullOrEmpty(currentPath) 
                        ? i.ToString() 
                        : $"{currentPath}{_pathSeparator}{i}";
                    
                    foreach (var path in GetAllPathsRecursive(jsonArray[i], newPath, remainingDepth - 1, pathCount, maxPathsPerLevel))
                    {
                        yield return path;
                        if (pathCount.Count >= maxPathsPerLevel)
                            yield break;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Returns the JSON representation of the root node.
    /// </summary>
    public override string ToString()
    {
        return _rootNode.ToJsonString();
    }
}

/// <summary>
/// Helper class to track path count across recursive calls (since ref parameters can't be used with iterators).
/// </summary>
internal class PathCounter
{
    public int Count { get; set; }
}

/// <summary>
/// Represents the result of a JSON navigation operation.
/// </summary>
internal record NavigationResult
{
    /// <summary>
    /// Gets whether the navigation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the JSON node found at the specified path (null if navigation failed).
    /// </summary>
    public JsonNode? Node { get; }

    /// <summary>
    /// Gets the path that was navigated.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the error message if navigation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private NavigationResult(bool isSuccess, JsonNode? node, string path, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        Node = node;
        Path = path;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful navigation result.
    /// </summary>
    public static NavigationResult Success(JsonNode? node, string path)
        => new(true, node, path);

    /// <summary>
    /// Creates a failed navigation result.
    /// </summary>
    public static NavigationResult Failure(string errorMessage)
        => new(false, null, string.Empty, errorMessage);

    public override string ToString()
    {
        return IsSuccess 
            ? $"Success: {Path} -> {Node}" 
            : $"Failed: {ErrorMessage}";
    }
}