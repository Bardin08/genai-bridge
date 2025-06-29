using System.Text.Json;
using GenAI.Bridge.Contracts;
using GenAI.Bridge.Scenarios;
using GenAI.Bridge.Scenarios.Models;
using GenAI.Bridge.Scenarios.Validation;
using GenAI.Bridge.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAI.Bridge.Tests.Scenarios;

public class ScenarioRegistrySchemaTests : IDisposable
{
    private readonly string _testScenariosDirectory;
    private readonly Mock<IScenarioValidator> _mockValidator;
    private readonly Mock<ILogger<ScenarioRegistry>> _mockLogger;

    public ScenarioRegistrySchemaTests()
    {
        _testScenariosDirectory = Path.Combine(Path.GetTempPath(), "ScenarioRegistryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testScenariosDirectory);
        
        _mockValidator = new Mock<IScenarioValidator>();
        _mockValidator.Setup(v => v.Validate(It.IsAny<ScenarioDefinition>()))
            .Returns(new ValidationResult());
            
        _mockLogger = new Mock<ILogger<ScenarioRegistry>>();
    }
    
    [Fact]
    public async Task GetScenario_WithTypeSchemaInResponseFormat_GeneratesSchemaCorrectly()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithTypeSchema";
        const string scenarioContent = """

                                       name: TestScenarioWithTypeSchema
                                       version: '1.0'
                                       description: 'Test scenario with C# type reference for schema'
                                       validModels: ['gpt-4', 'gpt-3.5-turbo']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: SchemaStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello, please provide customer information.'
                                               responseFormatConfig:
                                                 type: jsonSchema
                                                 responseTypeName: 'CustomerInfo'
                                       """;

        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act
        var scenario = await registry.GetScenario(scenarioName);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.Single(scenario.Stages);
        
        var userTurn = scenario.Stages[0].Turns[^1];
        Assert.Equal("Hello, please provide customer information.", userTurn.Content);
        
        var responseFormat = userTurn.Parameters!["response_format"] as ResponseFormat;
        Assert.NotNull(responseFormat);
        Assert.Equal(ResponseFormatType.JsonSchema, responseFormat.Type);
        Assert.NotNull(responseFormat.Schema);
        
        // Verify schema structure contains the CustomerInfo properties
        var schemaObj = JsonDocument.Parse(responseFormat.Schema).RootElement;
        var properties = schemaObj.GetProperty("schema").GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.True(properties.TryGetProperty("email", out _));
        Assert.True(properties.TryGetProperty("age", out _));
    }
    
    [Fact]
    public async Task GetScenario_WithExplicitSchema_UsesProvidedSchema()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithExplicitSchema";
        const string scenarioContent = """

                                       name: TestScenarioWithExplicitSchema
                                       version: '1.0'
                                       description: 'Test scenario with explicit JSON schema'
                                       validModels: ['gpt-4']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: SchemaStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello, please provide person information.'
                                               responseFormatConfig:
                                                 type: jsonSchema
                                                 schema: |
                                                     {
                                                         "type": "object",
                                                         "properties": {
                                                             "name": { "type": "string" },
                                                             "age": { "type": "integer" }
                                                         },
                                                         "required": ["name", "age"]
                                                     }
                                       """;

        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act
        var scenario = await registry.GetScenario(scenarioName);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.Single(scenario.Stages);
        
        var userTurn = scenario.Stages[0].Turns[^1];
        Assert.Equal("Hello, please provide person information.", userTurn.Content);
        
        var responseFormat = userTurn.Parameters!["response_format"] as ResponseFormat;
        Assert.NotNull(responseFormat);
        Assert.Equal(ResponseFormatType.JsonSchema, responseFormat.Type);
        
        // Verify the explicit schema was used
        var schemaObj = JsonDocument.Parse(responseFormat.Schema!).RootElement;
        Assert.Equal("object", schemaObj.GetProperty("type").GetString());
        var properties = schemaObj.GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.True(properties.TryGetProperty("age", out _));
    }
    
    [Fact]
    public async Task GetScenario_WithFunctionParametersType_GeneratesSchemaCorrectly()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithFunctionSchema";
        const string scenarioContent = """

                                       name: TestScenarioWithFunctionSchema
                                       version: '1.0'
                                       description: 'Test scenario with function schema from C# type'
                                       validModels: ['gpt-4']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: FunctionStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello, please book a flight.'
                                               parameters: {}
                                           functions:
                                             functions:
                                               - name: bookFlight
                                                 description: 'Book a flight for the user'
                                                 parametersType: 'FlightBooking'

                                             functionCall: auto
                                       """;

        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act
        var scenario = await registry.GetScenario(scenarioName);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.Single(scenario.Stages);

        var stage = scenario.Stages[0];

        var userTurn = stage.Turns[^1];
        Assert.Equal("Hello, please book a flight.", userTurn.Content);

        var functionsConfig = stage.Parameters!["functions"] as FunctionsConfig;
        Assert.NotNull(functionsConfig);
        Assert.Single(functionsConfig.Functions);

        var function = functionsConfig.Functions[0];
        Assert.Equal("bookFlight", function.Name);
        Assert.Equal("Book a flight for the user", function.Description);

        // Get the parameters JsonElement
        var parametersElement = (JsonElement)function.Parameters;
        var schema = parametersElement.GetProperty("schema");
        var propertiesSchema = schema.GetProperty("properties");

        // Verify the generated schema has the correct properties
        Assert.True(propertiesSchema.TryGetProperty("origin", out _));
        Assert.True(propertiesSchema.TryGetProperty("destination", out _));
        Assert.True(propertiesSchema.TryGetProperty("departureDate", out _));
        Assert.True(propertiesSchema.TryGetProperty("returnDate", out _));
        Assert.True(propertiesSchema.TryGetProperty("passengers", out _));
    }
    
    [Fact]
    public async Task GetScenario_WithToolParametersType_GeneratesSchemaCorrectly()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithToolSchema";
        const string scenarioContent = """

                                       name: TestScenarioWithToolSchema
                                       version: '1.0'
                                       description: 'Test scenario with tool schema from C# type'
                                       validModels: ['gpt-4']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: ToolStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello, please search for some information.'
                                               parameters: {}
                                           tools:
                                             - type: function
                                               function:
                                                 name: searchWeb
                                                 description: 'Search the web for information'
                                                 parametersType: 'SearchQuery'

                                       """;
        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act
        var scenario = await registry.GetScenario(scenarioName);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.Single(scenario.Stages);
        
        var stage = scenario.Stages[0];
        
        var userTurn = stage.Turns[^1];
        Assert.Equal("Hello, please search for some information.", userTurn.Content);
        
        var tools = stage.Parameters!["tools"] as List<Tool>;
        Assert.NotNull(tools);
        Assert.Single(tools);
        
        var tool = tools.First();
        Assert.Equal("function", tool.Type);
        Assert.Equal("searchWeb", tool.Function.Name);
        
        // Get the parameters JsonElement
        var parametersElement = (JsonElement)tool.Function.Parameters;
        var schema = parametersElement.GetProperty("schema");
        var properties = schema.GetProperty("properties");
        
        // Verify the generated schema has the correct properties
        Assert.True(properties.TryGetProperty("query", out _));
        Assert.True(properties.TryGetProperty("maxResults", out _));
        Assert.True(properties.TryGetProperty("includeImages", out _));
    }
    
    [Fact]
    public async Task GetScenario_WithMissingSchema_ThrowsException()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithMissingSchema";
        const string scenarioContent = """
                                       name: TestScenarioWithMissingSchema
                                       version: '1.0'
                                       description: 'Test scenario with missing schema'
                                       validModels: ['gpt-4']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: MissingSchemaStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello, please provide customer information.'
                                               responseFormatConfig:
                                                 type: json_schema
                                                 # Missing both schema and responseTypeName
                                       """;
        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await registry.GetScenario(scenarioName));
    }
    
    [Fact]
    public async Task GetScenario_WithInvalidSchemaType_UsesDefaultJsonSchema()
    {
        // Arrange
        const string scenarioName = "TestScenarioWithInvalidSchemaType";
        const string scenarioContent = """

                                       name: TestScenarioWithInvalidSchemaType
                                       version: '1.0'
                                       description: 'Test scenario with invalid schema type'
                                       validModels: ['gpt-4']
                                       metadata: 
                                         category: test
                                       stages:
                                         - name: InvalidSchemaStage
                                           systemPrompt: 'You are a helpful assistant.'
                                           userPrompts:
                                             - template: 'Hello.'
                                               responseFormatConfig:
                                                 type: jsonSchema
                                                 responseTypeName: 'NonExistentType'

                                       """;
        var filePath = Path.Combine(_testScenariosDirectory, $"{scenarioName}.yaml");
        await File.WriteAllTextAsync(filePath, scenarioContent);
        
        var registry = new ScenarioRegistry(
            _testScenariosDirectory, 
            _mockValidator.Object, 
            logger: _mockLogger.Object);
            
        // Act
        var scenario = await registry.GetScenario(scenarioName);
        
        // Assert
        Assert.NotNull(scenario);
        Assert.Single(scenario.Stages);
        
        var userTurn = scenario.Stages[0].Turns[^1];
        Assert.Equal("Hello.", userTurn.Content);
        
        var responseFormat = userTurn.Parameters!["response_format"] as ResponseFormat;
        Assert.NotNull(responseFormat);
        Assert.Equal(ResponseFormatType.JsonObject, responseFormat.Type);
        
        // When schema type cannot be resolved, it should use a default schema
        Assert.Null(responseFormat.Schema);
    }

    [Fact]
    public void TypeResolver_CanResolveTestTypes()
    {
        // This test verifies that TypeResolver can resolve the test types we're using
        // Arrange & Act & Assert
        var customerInfoType = TypeResolver.ResolveType(nameof(CustomerInfo));
        Assert.NotNull(customerInfoType);
        Assert.Equal(typeof(CustomerInfo), customerInfoType);
        
        var flightBookingType = TypeResolver.ResolveType(nameof(FlightBooking));
        Assert.NotNull(flightBookingType);
        Assert.Equal(typeof(FlightBooking), flightBookingType);
        
        var searchQueryType = TypeResolver.ResolveType(nameof(SearchQuery));
        Assert.NotNull(searchQueryType);
        Assert.Equal(typeof(SearchQuery), searchQueryType);
    }

    // Cleanup after tests
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testScenariosDirectory))
            {
                Directory.Delete(_testScenariosDirectory, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}

#region Test types used for schema generation

public class CustomerInfo
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class FlightBooking
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int Passengers { get; set; } = 1;
}

public class SearchQuery
{
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 10;
    public bool IncludeImages { get; set; } = false;
}

#endregion