# GenAI Bridge

A comprehensive bridge library for integrating various GenAI providers and services with unified interfaces for LLM, embeddings, vector storage, and context management.

## Features

- **Unified LLM Interface**: Consistent API for different LLM providers (OpenAI, etc.)
- **Embedding Support**: Standardized embedding generation and management
- **Vector Storage**: Integration with vector databases like Qdrant
- **Context Management**: Redis-based context storage and retrieval
- **Scenario Orchestration**: Dynamic scenario execution with middleware pipeline
- **Function Registry**: Tool and function management for LLM interactions

## Quick Start

```csharp
// Initialize the bridge
var bridge = new GenAiBridge();

// Configure providers
bridge.ConfigureOpenAI(apiKey: "your-api-key");

// Use the bridge
var result = await bridge.CompleteAsync("Hello, world!");
```

## Installation

```bash
dotnet add package GenAI.Bridge
```

## Documentation

For detailed documentation and examples, visit the [GitHub repository](https://github.com/your-username/genai-bridge).

## License

This project is licensed under the MIT License.
