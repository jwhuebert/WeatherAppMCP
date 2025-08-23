using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.McpServer.Models;

public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
    
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; set; }
}

public class McpError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object InputSchema { get; set; } = new { };
}

public class McpToolsListResult
{
    public List<McpTool> Tools { get; set; } = new();
}

public class McpToolCallParams
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Arguments { get; set; }
}

public class McpToolCallResult
{
    public List<McpContent> Content { get; set; } = new();
    public bool IsError { get; set; }
}

public class McpContent
{
    public string Type { get; set; } = "text";
    public string Text { get; set; } = string.Empty;
}