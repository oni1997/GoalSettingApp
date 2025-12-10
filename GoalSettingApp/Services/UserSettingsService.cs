using GoalSettingApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace GoalSettingApp.Services
{
    public class UserSettingsService
    {
        private readonly Supabase.Client _supabase;
        private readonly AuthenticationStateProvider _authStateProvider;

        public UserSettingsService(Supabase.Client supabase, AuthenticationStateProvider authStateProvider)
        {
            _supabase = supabase;
            _authStateProvider = authStateProvider;
        }

        private async Task<string?> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the user's settings, or creates default settings if none exist
        /// </summary>
        public async Task<UserSettings> GetUserSettingsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                // Return default settings for unauthenticated users
                return new UserSettings();
            }

            var response = await _supabase
                .From<UserSettings>()
                .Where(s => s.UserId == userId)
                .Get();

            var settings = response.Models.FirstOrDefault();

            if (settings == null)
            {
                // Create default settings for this user
                settings = new UserSettings
                {
                    UserId = userId,
                    PreferredCity = "Cape Town",
                    PreferredCityLat = -33.9249,
                    PreferredCityLon = 18.4241,
                    PreferredCountry = "ZA",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var insertResponse = await _supabase
                    .From<UserSettings>()
                    .Insert(settings);

                return insertResponse.Models.FirstOrDefault() ?? settings;
            }

            return settings;
        }

        /// <summary>
        /// Saves the user's preferred city
        /// </summary>
        public async Task<bool> SavePreferredCityAsync(string cityName, double lat, double lon, string country)
        {
            var userId = await GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var existingSettings = await GetUserSettingsAsync();

            existingSettings.PreferredCity = cityName;
            existingSettings.PreferredCityLat = lat;
            existingSettings.PreferredCityLon = lon;
            existingSettings.PreferredCountry = country;
            existingSettings.UpdatedAt = DateTime.UtcNow;

            await _supabase
                .From<UserSettings>()
                .Where(s => s.UserId == userId)
                .Update(existingSettings);

            return true;
        }
    }
}