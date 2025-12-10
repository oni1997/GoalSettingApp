using GoalSettingApp;

namespace GoalSettingApp.Tests;

public class GoalModelTests
{
    [Fact]
    public void Priority_GetSet_WorksCorrectly()
    {
        var goal = new Goal();

        goal.Priority = PriorityLevel.High;

        Assert.Equal(PriorityLevel.High, goal.Priority);
        Assert.Equal("High", goal.PriorityString);
    }

    [Fact]
    public void Priority_DefaultsToMedium()
    {
        var goal = new Goal();

        Assert.Equal(PriorityLevel.Medium, goal.Priority);
    }

    [Fact]
    public void Recurrence_GetSet_WorksCorrectly()
    {
        var goal = new Goal();

        goal.Recurrence = RecurrenceType.Weekly;

        Assert.Equal(RecurrenceType.Weekly, goal.Recurrence);
        Assert.Equal("Weekly", goal.RecurrenceString);
    }

    [Fact]
    public void Recurrence_DefaultsToNone()
    {
        var goal = new Goal();

        Assert.Equal(RecurrenceType.None, goal.Recurrence);
    }

    [Fact]
    public void IsRecurring_ReturnsFalse_WhenRecurrenceIsNone()
    {
        var goal = new Goal { Recurrence = RecurrenceType.None };

        Assert.False(goal.IsRecurring);
    }

    [Theory]
    [InlineData(RecurrenceType.Daily)]
    [InlineData(RecurrenceType.Weekly)]
    [InlineData(RecurrenceType.Monthly)]
    public void IsRecurring_ReturnsTrue_WhenRecurrenceIsSet(RecurrenceType recurrence)
    {
        var goal = new Goal { Recurrence = recurrence };

        Assert.True(goal.IsRecurring);
    }

    [Fact]
    public void CompletionCount_DefaultsToZero()
    {
        var goal = new Goal();

        Assert.Equal(0, goal.CompletionCount);
    }

    [Fact]
    public void IsCompleted_DefaultsToFalse()
    {
        var goal = new Goal();

        Assert.False(goal.IsCompleted);
    }
}
