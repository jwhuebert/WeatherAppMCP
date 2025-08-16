using Microsoft.AspNetCore.Mvc;
using Sample.MCP.API.Models;
using System.Text.Json;

namespace Sample.MCP.API.Controllers;

[ApiController]
[Route("mcp")]
public class McpController : ControllerBase
{
    private readonly ILogger<McpController> _logger;
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public McpController(ILogger<McpController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<McpResponse>> HandleMcpRequest([FromBody] McpRequest? request)
    {
        if (request == null)
        {
            _logger.LogError("MCP request is null or could not be deserialized");
            return BadRequest(new McpResponse
            {
                Error = new McpError { Code = -32700, Message = "Parse error: Invalid JSON" }
            });
        }
        
        _logger.LogInformation("MCP request received: {Method} (ID: {Id})", request.Method, request.Id?.ToString());
        
        try
        {
            var result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Id),
                "initialized" => HandleInitialized(request.Id),
                "tools/list" => HandleToolsList(request.Id),
                "tools/call" => await HandleToolCall(request.Id, request.Params),
                _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}"));
        }
    }

    private McpResponse HandleInitialize(JsonElement? requestId)
    {
        return new McpResponse
        {
            Id = requestId,
            Result = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new
                {
                    tools = new { }
                },
                serverInfo = new
                {
                    name = "weather-api",
                    version = "1.0.0"
                }
            }
        };
    }

    private McpResponse HandleInitialized(JsonElement? requestId)
    {
        return new McpResponse
        {
            Id = requestId,
            Result = new { }
        };
    }

    private McpResponse HandleToolsList(JsonElement? requestId)
    {
        var tools = new List<McpTool>
        {
            new()
            {
                Name = "get_weather_forecast",
                Description = "Get the weather forecast for the next 5 days with temperature and conditions",
                InputSchema = new 
                { 
                    type = "object", 
                    properties = new 
                    {
                        days = new
                        {
                            type = "integer",
                            description = "Number of days to forecast (1-10)",
                            @default = 5
                        }
                    }
                }
            },
            new()
            {
                Name = "get_temperature_stats",
                Description = "Get temperature statistics (min, max, average) for a weather forecast",
                InputSchema = new 
                { 
                    type = "object", 
                    properties = new 
                    {
                        days = new
                        {
                            type = "integer",
                            description = "Number of days to analyze (1-10)",
                            @default = 5
                        }
                    }
                }
            },
            new()
            {
                Name = "get_weather_summary",
                Description = "Get a text summary of the weather forecast including trends and notable conditions",
                InputSchema = new 
                { 
                    type = "object",
                    properties = new { }
                }
            }
        };

        return new McpResponse
        {
            Id = requestId,
            Result = new McpToolsListResult { Tools = tools }
        };
    }

    private async Task<McpResponse> HandleToolCall(JsonElement? requestId, JsonElement? parameters)
    {
        try
        {
            if (!parameters.HasValue)
            {
                return CreateErrorResponse(requestId, -32602, "Missing parameters");
            }
            
            var toolCall = JsonSerializer.Deserialize<McpToolCallParams>(parameters.Value.GetRawText(), new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (toolCall == null)
            {
                return CreateErrorResponse(requestId, -32602, "Invalid tool call parameters");
            }

            var result = toolCall.Name switch
            {
                "get_weather_forecast" => await GetWeatherForecast(toolCall.Arguments),
                "get_temperature_stats" => await GetTemperatureStats(toolCall.Arguments),
                "get_weather_summary" => await GetWeatherSummary(),
                _ => new McpToolCallResult 
                { 
                    Content = new List<McpContent> 
                    { 
                        new() { Text = $"Unknown tool: {toolCall.Name}" } 
                    }, 
                    IsError = true 
                }
            };

            return new McpResponse
            {
                Id = requestId,
                Result = result
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(requestId, -32603, $"Tool call failed: {ex.Message}");
        }
    }

    private Task<McpToolCallResult> GetWeatherForecast(Dictionary<string, object>? arguments)
    {
        var days = 5;
        if (arguments != null && arguments.TryGetValue("days", out var daysObj))
        {
            if (daysObj is JsonElement jsonDays && jsonDays.TryGetInt32(out var d))
            {
                days = Math.Clamp(d, 1, 10);
            }
            else if (int.TryParse(daysObj.ToString(), out var parsedDays))
            {
                days = Math.Clamp(parsedDays, 1, 10);
            }
        }

        var forecasts = GenerateForecasts(days);

        var formattedForecast = string.Join("\n", forecasts.Select((f, index) =>
        {
            var emoji = GetWeatherEmoji(f.Summary);
            return $"**{f.Date:dddd, MMM d}** {emoji}\n" +
                   $"  Temperature: {f.TemperatureC}°C ({f.TemperatureF}°F)\n" +
                   $"  Conditions: {f.Summary}";
        }));

        var message = $"Weather forecast for the next {forecasts.Count} days:\n\n{formattedForecast}";

        return Task.FromResult(new McpToolCallResult
        {
            Content = new List<McpContent> { new() { Text = message } }
        });
    }

    private Task<McpToolCallResult> GetTemperatureStats(Dictionary<string, object>? arguments)
    {
        var days = 5;
        if (arguments != null && arguments.TryGetValue("days", out var daysObj))
        {
            if (daysObj is JsonElement jsonDays && jsonDays.TryGetInt32(out var d))
            {
                days = Math.Clamp(d, 1, 10);
            }
            else if (int.TryParse(daysObj.ToString(), out var parsedDays))
            {
                days = Math.Clamp(parsedDays, 1, 10);
            }
        }

        var forecasts = GenerateForecasts(days);

        var temps = forecasts.Select(f => f.TemperatureC).ToList();
        var minTemp = temps.Min();
        var maxTemp = temps.Max();
        var avgTemp = temps.Average();

        var hottestDay = forecasts.OrderByDescending(f => f.TemperatureC).First();
        var coldestDay = forecasts.OrderBy(f => f.TemperatureC).First();

        var message = $"Temperature Statistics ({forecasts.Count} days):\n\n" +
                     $"🌡️ **Temperature Range**\n" +
                     $"  • Minimum: {minTemp}°C ({32 + (int)(minTemp / 0.5556)}°F)\n" +
                     $"  • Maximum: {maxTemp}°C ({32 + (int)(maxTemp / 0.5556)}°F)\n" +
                     $"  • Average: {avgTemp:F1}°C ({32 + (int)(avgTemp / 0.5556):F1}°F)\n\n" +
                     $"📅 **Notable Days**\n" +
                     $"  • Hottest: {hottestDay.Date:dddd, MMM d} - {hottestDay.TemperatureC}°C ({hottestDay.Summary})\n" +
                     $"  • Coldest: {coldestDay.Date:dddd, MMM d} - {coldestDay.TemperatureC}°C ({coldestDay.Summary})";

        return Task.FromResult(new McpToolCallResult
        {
            Content = new List<McpContent> { new() { Text = message } }
        });
    }

    private Task<McpToolCallResult> GetWeatherSummary()
    {
        var forecasts = GenerateForecasts(5);

        // Analyze weather patterns
        var conditions = forecasts.GroupBy(f => f.Summary)
            .OrderByDescending(g => g.Count())
            .ToList();

        var temps = forecasts.Select(f => f.TemperatureC).ToList();
        var avgTemp = temps.Average();

        // Determine trend
        var firstHalfAvg = forecasts.Take(forecasts.Count / 2).Average(f => f.TemperatureC);
        var secondHalfAvg = forecasts.Skip(forecasts.Count / 2).Average(f => f.TemperatureC);
        var trend = secondHalfAvg > firstHalfAvg ? "warming" : secondHalfAvg < firstHalfAvg ? "cooling" : "stable";

        var dominantCondition = conditions.First();
        var message = $"**Weather Summary**\n\n" +
                     $"The forecast shows {trend} temperatures over the next {forecasts.Count} days, " +
                     $"with an average of {avgTemp:F1}°C ({32 + (int)(avgTemp / 0.5556):F1}°F).\n\n" +
                     $"**Conditions**: {dominantCondition.Key} weather will be most common ({dominantCondition.Count()} days), ";

        if (conditions.Count > 1)
        {
            message += $"followed by {string.Join(", ", conditions.Skip(1).Select(c => $"{c.Key} ({c.Count()} days)"))}.\n\n";
        }
        else
        {
            message += "with consistent conditions throughout.\n\n";
        }

        // Add recommendations
        message += "**Recommendations**: ";
        if (avgTemp < 10)
            message += "Bundle up! Cold weather expected. ❄️";
        else if (avgTemp > 25)
            message += "Stay hydrated! Warm weather ahead. ☀️";
        else
            message += "Comfortable temperatures expected. Enjoy the weather! 🌤️";

        return Task.FromResult(new McpToolCallResult
        {
            Content = new List<McpContent> { new() { Text = message } }
        });
    }

    private List<WeatherForecast> GenerateForecasts(int days)
    {
        return Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToList();
    }

    private string GetWeatherEmoji(string? summary)
    {
        return summary?.ToLower() switch
        {
            "freezing" => "🥶",
            "bracing" => "❄️",
            "chilly" => "🌨️",
            "cool" => "🌤️",
            "mild" => "⛅",
            "warm" => "☀️",
            "balmy" => "🌞",
            "hot" => "🔥",
            "sweltering" => "🌡️",
            "scorching" => "☄️",
            _ => "🌡️"
        };
    }

    private McpResponse CreateErrorResponse(JsonElement? requestId, int code, string message)
    {
        return new McpResponse
        {
            Id = requestId,
            Error = new McpError { Code = code, Message = message }
        };
    }
}