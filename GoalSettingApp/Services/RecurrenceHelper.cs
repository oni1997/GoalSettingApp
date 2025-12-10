namespace GoalSettingApp.Services;

public static class RecurrenceHelper
{
    /// <summary>
    /// Calculates the next due date based on recurrence type
    /// </summary>
    public static DateTime? CalculateNextDueDate(DateTime? currentDueDate, RecurrenceType recurrence)
    {
        var baseDate = currentDueDate ?? DateTime.Today;

        // If the due date is in the past, calculate from today instead
        if (baseDate < DateTime.Today)
        {
            baseDate = DateTime.Today;
        }

        return recurrence switch
        {
            RecurrenceType.Daily => baseDate.AddDays(1),
            RecurrenceType.Weekly => baseDate.AddDays(7),
            RecurrenceType.Monthly => baseDate.AddMonths(1),
            _ => currentDueDate
        };
    }
}
