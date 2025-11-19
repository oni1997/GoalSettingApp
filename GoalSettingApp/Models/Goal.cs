namespace GoalSettingApp
{
    public class Goal
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public DateTime CreatedAt { get; set; }
        public bool IsCompleted { get; set; } = false;

    }

    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }
}

