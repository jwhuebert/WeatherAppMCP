# Weather MCP Server

A Model Context Protocol (MCP) server that provides weather forecast tools by calling the WeatherApp API.

## Features

- **get_weather_forecast**: Get a 5-day weather forecast
- **get_current_weather**: Get current weather (today's forecast)

## Building

```bash
# Build the project
dotnet build

# Publish as self-contained executable
dotnet publish -c Release
```

The executable will be created at: `bin/Release/net8.0/win-x64/publish/weather-mcp-server.exe`

## Configuration

The server connects to the WeatherApp API. By default, it uses `http://localhost:5000` but you can override this with the `WEATHER_API_URL` environment variable:

```bash
set WEATHER_API_URL=http://localhost:5001
weather-mcp-server.exe
```

## Usage

The MCP server communicates via JSON-RPC over stdin/stdout. It's designed to be used with MCP clients like Claude Desktop.

### MCP Configuration

Add to your MCP configuration:

```json
{
  "mcps": {
    "weather-forecast": {
      "command": "C:\\path\\to\\weather-mcp-server.exe",
      "args": []
    }
  }
}
```

## API Endpoints Called

The server calls these endpoints from the WeatherApp API:

- `GET /WeatherForecast` - Returns a 5-day weather forecast