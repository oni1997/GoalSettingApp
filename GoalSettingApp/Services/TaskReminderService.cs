using Microsoft.Extensions.DependencyInjection;

namespace GoalSettingApp.Services
{
    /// <summary>
    /// Background service that sends daily reminder emails at configured morning and evening times
    /// </summary>
    public class TaskReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskReminderService> _logger;
        private readonly HttpClient _httpClient;
        private readonly UserInfoCache _userCache;
        private readonly string _supabaseUrl;
        private readonly string _supabaseServiceKey;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute for precise timing
        private readonly TimeSpan _morningTime;
        private readonly TimeSpan _eveningTime;
        private DateTime _lastMorningReminder = DateTime.MinValue;
        private DateTime _lastEveningReminder = DateTime.MinValue;

        public TaskReminderService(
            IServiceProvider serviceProvider, 
            ILogger<TaskReminderService> logger, 
            IHttpClientFactory httpClientFactory,
            UserInfoCache userCache)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _userCache = userCache;

            _supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
            _supabaseServiceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY") ?? "";

            // Parse configured times from environment variables
            _morningTime = ParseTime(Environment.GetEnvironmentVariable("Morning_Time"), new TimeSpan(8, 0, 0));
            _eveningTime = ParseTime(Environment.GetEnvironmentVariable("Evening_Time"), new TimeSpan(20, 0, 0));

            _logger.LogInformation("Configured reminder times - Morning: {Morning}, Evening: {Evening}",
                _morningTime, _eveningTime);
        }

        private static TimeSpan ParseTime(string? timeString, TimeSpan defaultValue)
        {
            if (string.IsNullOrEmpty(timeString))
                return defaultValue;

            if (TimeSpan.TryParse(timeString, out var result))
                return result;

            return defaultValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Task Reminder Service started - Morning: {Morning}, Evening: {Evening}",
                _morningTime.ToString(@"hh\:mm"), _eveningTime.ToString(@"hh\:mm"));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendScheduledRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for task reminders");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Task Reminder Service stopped");
        }

        private async Task CheckAndSendScheduledRemindersAsync()
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var today = now.Date;

            // Check if it's time for morning reminder (within a 1-minute window)
            var isMorningTime = IsWithinTimeWindow(currentTime, _morningTime);
            var isEveningTime = IsWithinTimeWindow(currentTime, _eveningTime);

            // Morning reminder - only send once per day
            if (isMorningTime && _lastMorningReminder.Date != today)
            {
                _logger.LogInformation("Sending morning reminders at {Time}", currentTime);
                await SendDailyRemindersToAllUsersAsync(isMorning: true);
                _lastMorningReminder = now;
            }

            // Evening reminder - only send once per day
            if (isEveningTime && _lastEveningReminder.Date != today)
            {
                _logger.LogInformation("Sending evening reminders at {Time}", currentTime);
                await SendDailyRemindersToAllUsersAsync(isMorning: false);
                _lastEveningReminder = now;
            }
        }

        private static bool IsWithinTimeWindow(TimeSpan currentTime, TimeSpan targetTime)
        {
            // Allow a 1-minute window for the check
            var diff = Math.Abs((currentTime - targetTime).TotalMinutes);
            return diff < 1;
        }

        private async Task SendDailyRemindersToAllUsersAsync(bool isMorning)
        {
            using var scope = _serviceProvider.CreateScope();
            // Use the admin client (service role key) to bypass RLS and see all users' goals
            var supabase = scope.ServiceProvider.GetRequiredKeyedService<Supabase.Client>("admin");
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            try
            {
                // Get all incomplete goals from ALL users in the database
                var response = await supabase
                    .From<Goal>()
                    .Where(g => g.IsCompleted == false)
                    .Get();

                var goals = response.Models;
                _logger.LogInformation("Found {Count} incomplete goals across all users", goals.Count);

                // Group goals by user
                var goalsByUser = goals.GroupBy(g => g.UserId);

                foreach (var userGoals in goalsByUser)
                {
                    var userId = userGoals.Key;
                    var userPendingTasks = userGoals.ToList();

                    if (userPendingTasks.Count == 0) continue;

                    // Get user info from Supabase Auth (works for all users, not just logged in)
                    var (userEmail, userName) = await GetUserInfoAsync(userId);

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        await emailService.SendDailyReminderAsync(
                            userEmail,
                            userName ?? "User",
                            userPendingTasks,
                            isMorning
                        );

                        _logger.LogInformation(
                            "Sent {TimeOfDay} daily reminder to {Email} with {Count} tasks",
                            isMorning ? "morning" : "evening",
                            userEmail,
                            userPendingTasks.Count
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Could not get email for user {UserId}, skipping reminder", userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily reminders");
            }
        }

        private async Task<(string? email, string? name)> GetUserInfoAsync(string userId)
        {
            // Check cache first
            var cached = _userCache.Get(userId);
            if (cached.HasValue)
            {
                return (cached.Value.email, cached.Value.name);
            }

            try
            {
                // Use Supabase Admin API to get user info from auth.users
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_supabaseUrl}/auth/v1/admin/users/{userId}");
                request.Headers.Add("apikey", _supabaseServiceKey);
                request.Headers.Add("Authorization", $"Bearer {_supabaseServiceKey}");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userDoc = System.Text.Json.JsonDocument.Parse(json);
                    var root = userDoc.RootElement;

                    var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;

                    string? displayName = null;
                    if (root.TryGetProperty("user_metadata", out var metadata) &&
                        metadata.TryGetProperty("display_name", out var nameProp))
                    {
                        displayName = nameProp.GetString();
                    }

                    var result = (email, displayName ?? email);
                    
                    // Cache the result
                    _userCache.Set(userId, result.email, result.Item2);
                    
                    return result;
                }

                _logger.LogWarning("Failed to get user info for {UserId}: {StatusCode}", userId, response.StatusCode);
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info for {UserId}", userId);
                return (null, null);
            }
        }
    }
}

