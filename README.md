# Weather API as MCP Server

A simple example showing how to convert the .NET Weather API template into an MCP (Model Context Protocol) server that AI assistants can use.

## What is MCP?

MCP (Model Context Protocol) is a standard protocol that allows AI assistants like Claude to interact with external tools and APIs. This project demonstrates converting a basic .NET Web API into an MCP server.

## How It Works

The standard WeatherForecast API template is extended with an MCP controller that:

1. Handles MCP protocol initialization
2. Exposes available tools to the AI assistant
3. Processes tool calls and returns formatted responses

### Key Components

- **McpController.cs** - Main controller handling MCP protocol requests
- **McpModels.cs** - MCP protocol message models
- **WeatherForecast.cs** - Standard weather data model

### Available Tools

The MCP server exposes three weather-related tools:

- `get_weather_forecast` - Returns weather forecast for specified days
- `get_temperature_stats` - Provides temperature statistics (min/max/average)
- `get_weather_summary` - Generates a weather summary with trends and recommendations

## Running the Server

1. Build and run the API:
```bash
dotnet build
dotnet run
```

2. The MCP endpoint will be available at: `http://localhost:5047/mcp`

## Configuring with Claude Code

To add to Claude Code:

```bash
claude mcp add weather-forecast http://localhost:5047/mcp --transport http
```

Then run Claude as normal and ask for the forecast!

## MCP Protocol Flow

1. **Initialize**: Client establishes connection and gets server capabilities
2. **Tools List**: Client discovers available tools and their schemas
3. **Tool Call**: Client invokes specific tools with parameters
4. **Response**: Server executes logic and returns formatted results

## Learn More

- [MCP Documentation](https://modelcontextprotocol.io)
- [.NET Web API Documentation](https://docs.microsoft.com/en-us/aspnet/core/web-api/)

## License

This is a demonstration project for educational purposes.
