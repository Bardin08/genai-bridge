namespace GenAI.Bridge.Utils.Extensions;

internal static class CollectionsExtensions
{
    internal static IDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> src,
        IDictionary<TKey, TValue> dest) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(dest);

        foreach (var (k, v) in dest)
            src[k] = v;

        return src;
    }
    
    internal static IDictionary<TKey, TValue> MergeNoModify<TKey, TValue>(
        this IDictionary<TKey, TValue> src,
        IDictionary<TKey, TValue> dest) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(dest);

        var newDict = new Dictionary<TKey, TValue>(src);
        
        foreach (var (k, v) in dest)
            newDict.TryAdd(k, v);

        return newDict;
    }
}