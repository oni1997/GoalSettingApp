using GoalSettingApp.Models;

namespace GoalSettingApp.Services
{
    /// <summary>
    /// Background service that sends daily reminder emails at configured morning and evening times
    /// </summary>
    public class TaskReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute for precise timing
        private readonly TimeSpan _morningTime;
        private readonly TimeSpan _eveningTime;
        private DateTime _lastMorningReminder = DateTime.MinValue;
        private DateTime _lastEveningReminder = DateTime.MinValue;

        public TaskReminderService(IServiceProvider serviceProvider, ILogger<TaskReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

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
            var supabase = scope.ServiceProvider.GetRequiredService<Supabase.Client>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            try
            {
                // Get all incomplete goals
                var response = await supabase
                    .From<Goal>()
                    .Where(g => g.IsCompleted == false)
                    .Get();

                var goals = response.Models;

                // Group goals by user
                var goalsByUser = goals.GroupBy(g => g.UserId);

                foreach (var userGoals in goalsByUser)
                {
                    var userId = userGoals.Key;
                    var userPendingTasks = userGoals.ToList();

                    if (userPendingTasks.Count == 0) continue;

                    var userEmail = await GetUserEmailAsync(supabase, userId);
                    var userName = await GetUserNameAsync(supabase, userId);

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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send daily reminders");
            }
        }

        private async Task<string?> GetUserEmailAsync(Supabase.Client supabase, string userId)
        {
            try
            {
                // For now, we'll need to store user email in a profiles table or use admin API
                // This is a simplified approach - you may need to adjust based on your user data structure
                var response = await supabase
                    .From<UserProfile>()
                    .Where(p => p.UserId == userId)
                    .Single();

                return response?.Email;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GetUserNameAsync(Supabase.Client supabase, string userId)
        {
            try
            {
                var response = await supabase
                    .From<UserProfile>()
                    .Where(p => p.UserId == userId)
                    .Single();

                return response?.DisplayName;
            }
            catch
            {
                return null;
            }
        }
    }
}

