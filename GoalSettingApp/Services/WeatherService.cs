using System.Text.Json;
using GoalSettingApp.Model;

namespace GoalSettingApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/forecast";

        public WeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            //get API key from environment variable first, then from configuration
            _apiKey = Environment.GetEnvironmentVariable("API_KEY") 
                      ?? _configuration["OpenWeatherMap:ApiKey"] 
                      ?? string.Empty;
        }

        /// <summary>
        /// Gets weather forecast for a specific location
        /// </summary>
        /// <param name="latitude">Latitude of the location</param>
        /// <param name="longitude">Longitude of the location</param>
        /// <returns>Weather forecast response</returns>
        public async Task<WeatherForecastResponse?> GetForecastAsync(double latitude, double longitude)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API_KEY environment variable is not set");
            }

            var url = $"{BaseUrl}?lat={latitude}&lon={longitude}&appid={_apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var forecast = JsonSerializer.Deserialize<WeatherForecastResponse>(json);

                return forecast;
            }
            catch (HttpRequestException ex)
            {
                // Log the error (in a real app, use proper logging)
                Console.WriteLine($"Error fetching weather data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets weather forecast for Cape Town (default location)
        /// </summary>
        /// <returns>Weather forecast response</returns>
        public async Task<WeatherForecastResponse?> GetCapeTownForecastAsync()
        {
            // Cape Town coordinates
            const double latitude = -33.9249;
            const double longitude = 18.4241;

            return await GetForecastAsync(latitude, longitude);
        }

        /// <summary>
        /// Gets simplified daily forecast (one entry per day)
        /// </summary>
        /// <param name="latitude">Latitude of the location</param>
        /// <param name="longitude">Longitude of the location</param>
        /// <returns>List of daily weather data</returns>
        public async Task<List<WeatherData>> GetDailyForecastAsync(double latitude, double longitude)
        {
            var forecast = await GetForecastAsync(latitude, longitude);

            if (forecast?.List == null)
            {
                return new List<WeatherData>();
            }

            // Group by date and take the midday forecast (12:00) for each day
            var dailyForecasts = forecast.List
                .GroupBy(w => w.DateTime.Date)
                .Select(g => g.OrderBy(w => Math.Abs((w.DateTime.Hour - 12))).First())
                .Take(5) // Get 5 days
                .ToList();

            return dailyForecasts;
        }

        /// <summary>
        /// Gets simplified daily forecast for Cape Town
        /// </summary>
        /// <returns>List of daily weather data</returns>
        public async Task<List<WeatherData>> GetCapeTownDailyForecastAsync()
        {
            const double latitude = -33.9249;
            const double longitude = 18.4241;

            return await GetDailyForecastAsync(latitude, longitude);
        }
    }
}

