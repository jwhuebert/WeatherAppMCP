using System.Text.Json;
using WeatherApp.McpServer.Models;

namespace WeatherApp.McpServer.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<List<WeatherForecast>> GetWeatherForecastAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/WeatherForecast");
                response.EnsureSuccessStatusCode();
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                var forecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(jsonContent, _jsonOptions);
                
                return forecasts ?? new List<WeatherForecast>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get weather forecast: {ex.Message}");
            }
        }

        public async Task<WeatherForecast?> GetCurrentWeatherAsync()
        {
            try
            {
                var forecasts = await GetWeatherForecastAsync();
                return forecasts.FirstOrDefault(); // Return the first forecast as "current"
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get current weather: {ex.Message}");
            }
        }
    }
}