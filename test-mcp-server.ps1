# Test script for WeatherApp MCP Server
param(
    [string]$McpServerPath = "C:\git\WeatherAppMCP\WeatherApp.McpServer\bin\Release\net8.0\win-x64\publish\weather-mcp-server.exe",
    [string]$WeatherApiUrl = "http://localhost:5047"
)

Write-Host "=== WeatherApp MCP Server Test ===" -ForegroundColor Cyan

# Set environment
$env:WEATHER_API_URL = $WeatherApiUrl

# Test function
function Test-SingleMcpRequest {
    param(
        [string]$TestName,
        [string]$JsonRequest
    )
    
    Write-Host ""
    Write-Host "--- $TestName ---" -ForegroundColor Yellow
    Write-Host "Request: $JsonRequest" -ForegroundColor Gray
    
    try {
        # Create process
        $process = New-Object System.Diagnostics.ProcessStartInfo
        $process.FileName = $McpServerPath
        $process.UseShellExecute = $false
        $process.RedirectStandardInput = $true
        $process.RedirectStandardOutput = $true
        $process.RedirectStandardError = $true
        $process.CreateNoWindow = $true
        
        $proc = [System.Diagnostics.Process]::Start($process)
        
        # Send request
        $proc.StandardInput.WriteLine($JsonRequest)
        $proc.StandardInput.Close()
        
        # Wait for completion with timeout
        if ($proc.WaitForExit(5000)) {
            $output = $proc.StandardOutput.ReadToEnd()
            $error = $proc.StandardError.ReadToEnd()
            
            if ($output.Trim()) {
                Write-Host "Success" -ForegroundColor Green
                Write-Host "Response: $output" -ForegroundColor Cyan
            } else {
                Write-Host "— No output" -ForegroundColor Red
                if ($error.Trim()) {
                    Write-Host "Error: $error" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "âœ— Timeout" -ForegroundColor Red
            $proc.Kill()
        }
        
        $proc.Dispose()
        
    } catch {
        Write-Host "— Exception: $_" -ForegroundColor Red
    }
}

# Check if Weather API is running
Write-Host "Checking if Weather API is accessible..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$WeatherApiUrl/WeatherForecast" -Method GET -TimeoutSec 5
    Write-Host "Weather API is accessible" -ForegroundColor Green
} catch {
    Write-Warning "Weather API not accessible at $WeatherApiUrl"
    Write-Host "Make sure the WeatherApp API is running first:" -ForegroundColor Yellow
    Write-Host "  cd Sample.MCP.API" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
}

# Run Tests
Test-SingleMcpRequest "Initialize" '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'

Test-SingleMcpRequest "List Tools" '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'

Test-SingleMcpRequest "Get Weather Forecast" '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_weather_forecast","arguments":{}}}'

Test-SingleMcpRequest "Get Current Weather" '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"get_current_weather","arguments":{}}}'

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan