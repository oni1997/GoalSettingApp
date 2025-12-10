using Microsoft.Extensions.DependencyInjection;

namespace GoalSettingApp.Services
{
    /// <summary>
    /// Background service that checks and resets recurring goals based on their frequency
    /// </summary>
    public class RecurringGoalService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecurringGoalService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public RecurringGoalService(IServiceProvider serviceProvider, ILogger<RecurringGoalService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recurring Goal Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecurringGoalsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing recurring goals");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Recurring Goal Service stopped");
        }

        private async Task ProcessRecurringGoalsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var supabase = scope.ServiceProvider.GetRequiredKeyedService<Supabase.Client>("admin");

            try
            {
                // Get all completed recurring goals
                var response = await supabase
                    .From<Goal>()
                    .Where(g => g.IsCompleted == true)
                    .Get();

                var recurringGoals = response.Models
                    .Where(g => g.Recurrence != RecurrenceType.None)
                    .ToList();

                _logger.LogInformation("Found {Count} completed recurring goals to check", recurringGoals.Count);

                int resetCount = 0;
                foreach (var goal in recurringGoals)
                {
                    // Reset completed recurring goals
                    // Reset the goal
                    goal.IsCompleted = false;
                    goal.CompletionCount++;

                    // Update due date based on frequency
                    if (goal.DueDate.HasValue)
                    {
                        goal.DueDate = goal.Recurrence switch
                        {
                            RecurrenceType.Daily => DateTime.Today.AddDays(1),
                            RecurrenceType.Weekly => DateTime.Today.AddDays(7),
                            RecurrenceType.Monthly => DateTime.Today.AddMonths(1),
                            _ => goal.DueDate
                        };
                    }

                    await supabase
                        .From<Goal>()
                        .Where(g => g.Id == goal.Id)
                        .Update(goal);

                    resetCount++;
                    _logger.LogInformation(
                        "Reset recurring goal: {Title} (Recurrence: {Recurrence})",
                        goal.Title,
                        goal.Recurrence
                    );
                }

                if (resetCount > 0)
                {
                    _logger.LogInformation("Successfully reset {Count} recurring goals", resetCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process recurring goals");
            }
        }
    }
}
