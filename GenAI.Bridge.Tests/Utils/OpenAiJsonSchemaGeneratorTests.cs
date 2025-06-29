using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenAI.Bridge.Utils;

namespace GenAI.Bridge.Tests.Utils;

public class OpenAiJsonSchemaGeneratorTests
{
    [Fact]
    public void GenerateSchema_SimpleType_ReturnsValidSchema()
    {
        // Arrange
        var schemaName = "SimpleTypeSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<SimpleType>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        Assert.Equal(schemaName, root.GetProperty("name").GetString());
        Assert.True(root.GetProperty("strict").GetBoolean());

        var schemaObj = root.GetProperty("schema");
        Assert.Equal("object", schemaObj.GetProperty("type").GetString());

        var properties = schemaObj.GetProperty("properties");
        Assert.True(properties.TryGetProperty("stringProperty", out var stringProp));
        Assert.Equal("string", stringProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("intProperty", out var intProp));
        Assert.Equal("integer", intProp.GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("boolProperty", out var boolProp));
        Assert.Equal("boolean", boolProp.GetProperty("type").GetString());

        var required = schemaObj.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("stringProperty", required);
        Assert.Contains("intProperty", required);
        Assert.Contains("boolProperty", required);
    }

    [Fact]
    public void GenerateSchema_WithEnumType_ReturnsValidSchema()
    {
        // Arrange
        var schemaName = "EnumTypeSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithEnum>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var schemaObj = root.GetProperty("schema");
        var properties = schemaObj.GetProperty("properties");
        Assert.True(properties.TryGetProperty("status", out var statusProp));
        Assert.Equal("string", statusProp.GetProperty("type").GetString());

        var enumValues = statusProp.GetProperty("enum").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(3, enumValues.Length);
        Assert.Contains("Pending", enumValues);
        Assert.Contains("Active", enumValues);
        Assert.Contains("Completed", enumValues);
    }

    [Fact]
    public void GenerateSchema_WithNestedObject_ReturnsValidSchema()
    {
        // Arrange
        var schemaName = "NestedTypeSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<ParentType>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var schemaObj = root.GetProperty("schema");
        var properties = schemaObj.GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.True(properties.TryGetProperty("child", out var childProp));

        // Check that definitions were created
        Assert.True(schemaObj.TryGetProperty("$defs", out var defs));
        var hasValidDefs = defs.EnumerateObject().Any(x => x.Name == "ChildType");
        Assert.True(hasValidDefs);
    }

    [Fact]
    public void GenerateSchema_WithArrayProperty_ReturnsValidSchema()
    {
        // Arrange
        const string schemaName = "ArrayTypeSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithArray>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var schemaObj = root.GetProperty("schema");
        var properties = schemaObj.GetProperty("properties");
        Assert.True(properties.TryGetProperty("items", out var itemsProp));
        Assert.Equal("array", itemsProp.GetProperty("type").GetString());
        Assert.Equal("string", itemsProp.GetProperty("items").GetProperty("type").GetString());

        Assert.True(properties.TryGetProperty("complexItems", out var complexItemsProp));
        Assert.Equal("array", complexItemsProp.GetProperty("type").GetString());
    }

    [Fact]
    public void GenerateSchema_WithValidationAttributes_IncludesConstraints()
    {
        // Arrange
        var schemaName = "ValidationSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithValidation>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var schemaObj = root.GetProperty("schema");
        var properties = schemaObj.GetProperty("properties");

        // String length constraints
        Assert.True(properties.TryGetProperty("name", out var nameProp));
        Assert.Equal(3, nameProp.GetProperty("minLength").GetInt32());
        Assert.Equal(50, nameProp.GetProperty("maxLength").GetInt32());

        // Regex pattern constraint
        Assert.True(properties.TryGetProperty("email", out var emailProp));
        Assert.True(emailProp.TryGetProperty("pattern", out _));

        // Range constraints
        Assert.True(properties.TryGetProperty("age", out var ageProp));
        Assert.Equal(18, ageProp.GetProperty("minimum").GetInt32());
        Assert.Equal(120, ageProp.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public void GenerateSchema_WithDescription_IncludesDescriptionInSchema()
    {
        // Arrange
        var schemaName = "DescriptionSchema";
        var expectedDescription = "Schema with descriptions";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithDescriptions>(schemaName, expectedDescription);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        // Check the root schema description
        Assert.Equal(expectedDescription, root.GetProperty("description").GetString());

        // Check property descriptions
        var properties = root.GetProperty("schema").GetProperty("properties");
        Assert.True(properties.TryGetProperty("title", out var titleProp));
        Assert.Equal("The title of the item", titleProp.GetProperty("description").GetString());
    }

    [Fact]
    public void GenerateSchema_WithJsonPropertyNames_UsesCustomPropertyNames()
    {
        // Arrange
        var schemaName = "CustomNamesSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithCustomNames>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var properties = root.GetProperty("schema").GetProperty("properties");

        // Should have custom property name instead of C# property name
        Assert.True(properties.TryGetProperty("custom_id", out _));
        Assert.True(properties.TryGetProperty("full_name", out _));

        // Should not have the original C# property names
        Assert.False(properties.TryGetProperty("Id", out _));
        Assert.False(properties.TryGetProperty("FullName", out _));
    }

    [Fact]
    public void GenerateSchema_WithNullableTypes_SupportsNullValues()
    {
        // Arrange
        var schemaName = "NullableSchema";

        // Act
        var schema = OpenAiJsonSchemaGenerator.GenerateSchema<TypeWithNullable>(schemaName);

        // Assert
        var jsonDocument = JsonDocument.Parse(schema);
        var root = jsonDocument.RootElement;

        var properties = root.GetProperty("schema").GetProperty("properties");

        // Check nullable property has anyOf with null option
        Assert.True(properties.TryGetProperty("optionalValue", out var optionalProp));
        Assert.True(optionalProp.TryGetProperty("anyOf", out var anyOf));

        var anyOfArray = anyOf.EnumerateArray().ToArray();
        Assert.Equal(2, anyOfArray.Length);

        // One option should be the actual type
        var firstOption = anyOfArray[0];
        Assert.Equal("integer", firstOption.GetProperty("type").GetString());

        // Other option should be null
        var secondOption = anyOfArray[1];
        Assert.Equal("null", secondOption.GetProperty("type").GetString());
    }

    #region Helper Classes

    private class SimpleType
    {
        public string StringProperty { get; set; } = string.Empty;
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
    }

    private enum Status
    {
        Pending,
        Active,
        Completed
    }

    private class TypeWithEnum
    {
        public Status Status { get; set; }
    }

    private class ChildType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ParentType
    {
        public string Name { get; set; } = string.Empty;
        public ChildType Child { get; set; } = new();
    }

    private class TypeWithArray
    {
        public string[] Items { get; set; } = Array.Empty<string>();
        public List<ChildType> ComplexItems { get; set; } = new();
    }

    private class TypeWithValidation
    {
        [StringLength(50, MinimumLength = 3)] public string Name { get; set; } = string.Empty;

        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        public string Email { get; set; } = string.Empty;

        [Range(18, 120)] public int Age { get; set; }
    }

    [Description("A type with descriptions")]
    private class TypeWithDescriptions
    {
        [Description("The title of the item")] public string Title { get; set; } = string.Empty;

        [Display(Description = "The description of the item using Display attribute")]
        public string Description { get; set; } = string.Empty;
    }

    private class TypeWithCustomNames
    {
        [JsonPropertyName("custom_id")] public int Id { get; set; }

        [JsonPropertyName("full_name")] public string FullName { get; set; } = string.Empty;
    }

    private class TypeWithNullable
    {
        public int? OptionalValue { get; set; }
    }

    # endregion
}