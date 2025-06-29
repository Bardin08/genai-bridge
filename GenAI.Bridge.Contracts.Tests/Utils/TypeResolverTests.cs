using System.Reflection;
using GenAI.Bridge.Utils;

namespace GenAI.Bridge.Contracts.Tests.Utils;

public class TypeResolverTests
{
    [Fact]
    public void ResolveType_WithValidTypeName_ReturnsCorrectType()
    {
        // Arrange
        const string typeName = nameof(DummyTestClass);

        // Act
        var resolvedType = TypeResolver.ResolveType(typeName);

        // Assert
        Assert.NotNull(resolvedType);
        Assert.Equal(typeof(DummyTestClass), resolvedType);
    }

    [Fact]
    public void ResolveType_WithFullyQualifiedName_ReturnsCorrectType()
    {
        // Arrange
        var typeName = typeof(DummyTestClass).FullName!;

        // Act
        var resolvedType = TypeResolver.ResolveType(typeName);

        // Assert
        Assert.NotNull(resolvedType);
        Assert.Equal(typeof(DummyTestClass), resolvedType);
    }

    [Fact]
    public void ResolveType_WithInvalidTypeName_ReturnsNull()
    {
        // Arrange
        const string typeName = "NonExistentTypeName";

        // Act
        var resolvedType = TypeResolver.ResolveType(typeName);

        // Assert
        Assert.Null(resolvedType);
    }

    [Fact]
    public void ResolveType_WithEmptyTypeName_ReturnsNull()
    {
        // Arrange
        var typeName = string.Empty;

        // Act
        var resolvedType = TypeResolver.ResolveType(typeName);

        // Assert
        Assert.Null(resolvedType);
    }

    [Fact]
    public void ResolveType_WithNullTypeName_ReturnsNull()
    {
        // Act
        var resolvedType = TypeResolver.ResolveType(null!);

        // Assert
        Assert.Null(resolvedType);
    }

    [Fact]
    public void ResolveType_WithCaseInsensitiveTypeName_ReturnsCorrectType()
    {
        // Arrange
        var typeName = nameof(DummyTestClass).ToLowerInvariant();

        // Act
        var resolvedType = TypeResolver.ResolveType(typeName);

        // Assert
        Assert.NotNull(resolvedType);
        Assert.Equal(typeof(DummyTestClass), resolvedType);
    }

    [Fact]
    public void GenerateSchemaFromTypeName_WithValidTypeName_ReturnsSchema()
    {
        // Arrange
        const string typeName = nameof(DummyTestClass);

        // Act
        var schema = TypeResolver.GenerateSchemaFromTypeName(typeName);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("TestClass", schema);
        Assert.Contains("StringProperty", schema);
        Assert.Contains("IntProperty", schema);
    }

    [Fact]
    public void GenerateSchemaFromTypeName_WithInvalidTypeName_ReturnsNull()
    {
        // Arrange
        var typeName = "NonExistentTypeName";

        // Act
        var schema = TypeResolver.GenerateSchemaFromTypeName(typeName);

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public void GenerateSchemaFromTypeName_WithEmptyTypeName_ReturnsNull()
    {
        // Act
        var schema = TypeResolver.GenerateSchemaFromTypeName(string.Empty);

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public void GenerateSchemaFromTypeName_WithNullTypeName_ReturnsNull()
    {
        // Act
        var schema = TypeResolver.GenerateSchemaFromTypeName(null!);

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public void RefreshTypeCache_ResetsAndRebuildsCache()
    {
        // Arrange
        // First make sure our test type is resolved once to have it in the cache
        var typeName = nameof(DummyTestClass);
        var initialResolvedType = TypeResolver.ResolveType(typeName);
        Assert.NotNull(initialResolvedType);

        // Act
        TypeResolver.RefreshTypeCache();

        // Assert
        // The type should still be resolvable after a cache refresh
        var refreshedResolvedType = TypeResolver.ResolveType(typeName);
        Assert.NotNull(refreshedResolvedType);
        Assert.Equal(typeof(DummyTestClass), refreshedResolvedType);
    }

    [Fact]
    public void TypeResolver_CorrectlyIdentifiesSystemAssemblies()
    {
        // We need to use reflection to test the private IsSystemAssembly method
        var methodInfo = typeof(TypeResolver).GetMethod("IsSystemAssembly",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(methodInfo);

        // Test system assemblies
        var systemAssemblies = new[]
        {
            typeof(string).Assembly, // mscorlib
            typeof(System.Linq.Enumerable).Assembly, // System.Linq
            typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly // Microsoft.CSharp
        };

        foreach (var assembly in systemAssemblies)
        {
            var result = (bool)methodInfo.Invoke(null, new object[] { assembly })!;
            Assert.True(result, $"Assembly {assembly.GetName().Name} should be identified as a system assembly");
        }

        // Test non-system assemblies
        var nonSystemAssemblies = new[]
        {
            typeof(TypeResolver).Assembly, // GenAI.Bridge
            Assembly.GetExecutingAssembly() // The test assembly
        };

        foreach (var assembly in nonSystemAssemblies)
        {
            var result = (bool)methodInfo.Invoke(null, new object[] { assembly })!;
            Assert.False(result, $"Assembly {assembly.GetName().Name} should not be identified as a system assembly");
        }
    }

    [Fact]
    public void TypeResolver_ResolvesCachedTypesFast()
    {
        // Arrange - resolve a type first to cache it
        const string typeName = nameof(DummyTestClass);
        TypeResolver.ResolveType(typeName);

        // Act - resolve it again and measure time (should be fast)
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var resolvedType = TypeResolver.ResolveType(typeName);
        watch.Stop();

        // Assert
        Assert.NotNull(resolvedType);
        Assert.Equal(typeof(DummyTestClass), resolvedType);

        // This is a smoke test for performance - it should be extremely fast from cache,
        // but we don't want to be too strict with the threshold to avoid flaky tests
        Assert.True(watch.ElapsedMilliseconds < 10,
            $"Resolving a cached type took {watch.ElapsedMilliseconds}ms, which is longer than expected");
    }
}

// Test support class
public class DummyTestClass
{
    public string StringProperty { get; set; } = string.Empty;
    public int IntProperty { get; set; }
    public bool BoolProperty { get; set; }
}