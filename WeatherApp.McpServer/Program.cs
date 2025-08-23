using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherApp.McpServer.Models;
using WeatherApp.McpServer.Services;

namespace WeatherApp.McpServer
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string apiBaseUrl = Environment.GetEnvironmentVariable("WEATHER_API_URL") ?? "http://localhost:5000";
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static async Task Main(string[] args)
        {
            // Configure HTTP client
            httpClient.BaseAddress = new Uri(apiBaseUrl);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Read from stdin and write to stdout for MCP communication
            using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
            using var writer = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8)
            {
                AutoFlush = true
            };

            var weatherService = new WeatherService(httpClient);

            while (true)
            {
                try
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var request = JsonSerializer.Deserialize<McpRequest>(line, jsonOptions);
                    if (request == null)
                        continue;

                    var response = await HandleRequest(request, weatherService);
                    var responseJson = JsonSerializer.Serialize(response, jsonOptions);
                    await writer.WriteLineAsync(responseJson);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    await Console.Error.WriteLineAsync($"Error processing request: {ex.Message}");
                }
            }
        }

        private static async Task<McpResponse> HandleRequest(McpRequest request, WeatherService weatherService)
        {
            var response = new McpResponse
            {
                JsonRpc = "2.0",
                Id = request.Id
            };

            try
            {
                switch (request.Method)
                {
                    case "initialize":
                        response.Result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new
                            {
                                tools = new { }
                            },
                            serverInfo = new
                            {
                                name = "weather-forecast-mcp",
                                version = "1.0.0"
                            }
                        };
                        break;

                    case "tools/list":
                        response.Result = new McpToolsListResult
                        {
                            Tools = new List<McpTool>
                            {
                                new McpTool
                                {
                                    Name = "get_weather_forecast",
                                    Description = "Get weather forecast for the next 5 days",
                                    InputSchema = new
                                    {
                                        type = "object",
                                        properties = new { },
                                        required = Array.Empty<string>()
                                    }
                                },
                                new McpTool
                                {
                                    Name = "get_current_weather",
                                    Description = "Get current weather (today's forecast)",
                                    InputSchema = new
                                    {
                                        type = "object",
                                        properties = new { },
                                        required = Array.Empty<string>()
                                    }
                                }
                            }
                        };
                        break;

                    case "tools/call":
                        if (request.Params.HasValue)
                        {
                            var toolCall = JsonSerializer.Deserialize<McpToolCallParams>(
                                request.Params.Value.GetRawText(), jsonOptions);
                            
                            if (toolCall != null)
                            {
                                response.Result = await HandleToolCall(toolCall, weatherService);
                            }
                            else
                            {
                                response.Error = new McpError
                                {
                                    Code = -32602,
                                    Message = "Invalid params"
                                };
                            }
                        }
                        else
                        {
                            response.Error = new McpError
                            {
                                Code = -32602,
                                Message = "Missing params"
                            };
                        }
                        break;

                    default:
                        response.Error = new McpError
                        {
                            Code = -32601,
                            Message = "Method not found"
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                };
            }

            return response;
        }

        private static async Task<McpToolCallResult> HandleToolCall(McpToolCallParams toolCall, WeatherService weatherService)
        {
            switch (toolCall.Name)
            {
                case "get_weather_forecast":
                    var forecasts = await weatherService.GetWeatherForecastAsync();
                    return new McpToolCallResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = FormatWeatherForecasts(forecasts)
                            }
                        }
                    };

                case "get_current_weather":
                    var currentWeather = await weatherService.GetCurrentWeatherAsync();
                    return new McpToolCallResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = FormatCurrentWeather(currentWeather)
                            }
                        }
                    };

                default:
                    return new McpToolCallResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent
                            {
                                Type = "text",
                                Text = $"Unknown tool: {toolCall.Name}"
                            }
                        },
                        IsError = true
                    };
            }
        }

        private static string FormatWeatherForecasts(List<WeatherForecast> forecasts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("5-Day Weather Forecast:");
            sb.AppendLine("========================");
            
            foreach (var forecast in forecasts)
            {
                sb.AppendLine($"Date: {forecast.Date:yyyy-MM-dd}");
                sb.AppendLine($"Temperature: {forecast.TemperatureC}°C ({forecast.TemperatureF}°F)");
                sb.AppendLine($"Conditions: {forecast.Summary}");
                sb.AppendLine("------------------------");
            }
            
            return sb.ToString();
        }

        private static string FormatCurrentWeather(WeatherForecast? weather)
        {
            if (weather == null)
                return "No current weather data available";

            var sb = new StringBuilder();
            sb.AppendLine("Current Weather:");
            sb.AppendLine("================");
            sb.AppendLine($"Date: {weather.Date:yyyy-MM-dd}");
            sb.AppendLine($"Temperature: {weather.TemperatureC}°C ({weather.TemperatureF}°F)");
            sb.AppendLine($"Conditions: {weather.Summary}");
            
            return sb.ToString();
        }
    }
}
