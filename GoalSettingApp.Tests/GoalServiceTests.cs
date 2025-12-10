using GoalSettingApp;
using GoalSettingApp.Services;

namespace GoalSettingApp.Tests;

public class RecurrenceHelperTests
{
    [Fact]
    public void CalculateNextDueDate_Daily_AddsOneDay()
    {
        var currentDate = new DateTime(2025, 12, 10);

        var result = RecurrenceHelper.CalculateNextDueDate(currentDate, RecurrenceType.Daily);

        Assert.Equal(new DateTime(2025, 12, 11), result);
    }

    [Fact]
    public void CalculateNextDueDate_Weekly_AddsSevenDays()
    {
        var currentDate = new DateTime(2025, 12, 10);

        var result = RecurrenceHelper.CalculateNextDueDate(currentDate, RecurrenceType.Weekly);

        Assert.Equal(new DateTime(2025, 12, 17), result);
    }

    [Fact]
    public void CalculateNextDueDate_Monthly_AddsOneMonth()
    {
        var currentDate = new DateTime(2025, 12, 10);

        var result = RecurrenceHelper.CalculateNextDueDate(currentDate, RecurrenceType.Monthly);

        Assert.Equal(new DateTime(2026, 1, 10), result);
    }

    [Fact]
    public void CalculateNextDueDate_None_ReturnsOriginalDate()
    {
        var currentDate = new DateTime(2025, 12, 10);

        var result = RecurrenceHelper.CalculateNextDueDate(currentDate, RecurrenceType.None);

        Assert.Equal(currentDate, result);
    }

    [Fact]
    public void CalculateNextDueDate_NullDate_UsesToday()
    {
        var result = RecurrenceHelper.CalculateNextDueDate(null, RecurrenceType.Daily);

        // Should be tomorrow (Today + 1 day)
        Assert.Equal(DateTime.Today.AddDays(1), result);
    }

    [Fact]
    public void CalculateNextDueDate_PastDate_CalculatesFromToday()
    {
        var pastDate = DateTime.Today.AddDays(-10);

        var result = RecurrenceHelper.CalculateNextDueDate(pastDate, RecurrenceType.Daily);

        // Should calculate from today, not from the past date
        Assert.Equal(DateTime.Today.AddDays(1), result);
    }

    [Fact]
    public void CalculateNextDueDate_FutureDate_CalculatesFromThatDate()
    {
        var futureDate = DateTime.Today.AddDays(5);

        var result = RecurrenceHelper.CalculateNextDueDate(futureDate, RecurrenceType.Weekly);

        Assert.Equal(futureDate.AddDays(7), result);
    }

    [Fact]
    public void CalculateNextDueDate_MonthlyEndOfMonth_HandlesCorrectly()
    {
        // January 31st + 1 month should be February 28th (or 29th in leap year)
        // Use a future date to avoid the "past date recalculates from today" logic
        var endOfJan = new DateTime(2026, 1, 31);

        var result = RecurrenceHelper.CalculateNextDueDate(endOfJan, RecurrenceType.Monthly);

        Assert.Equal(new DateTime(2026, 2, 28), result);
    }

    [Theory]
    [InlineData(RecurrenceType.Daily, 1)]
    [InlineData(RecurrenceType.Weekly, 7)]
    public void CalculateNextDueDate_VariousRecurrences_AddsCorrectDays(RecurrenceType recurrence, int expectedDays)
    {
        // Use a future date to avoid the "past date recalculates from today" logic
        var startDate = DateTime.Today.AddDays(30);

        var result = RecurrenceHelper.CalculateNextDueDate(startDate, recurrence);

        Assert.Equal(startDate.AddDays(expectedDays), result);
    }
}
