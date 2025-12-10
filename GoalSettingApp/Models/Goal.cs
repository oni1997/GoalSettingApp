using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace GoalSettingApp
{
    [Table("goals")]
    public class Goal : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("priority")]
        [JsonProperty("priority")]
        public string PriorityString { get; set; } = "Medium";

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [Column("recurrence")]
        [JsonProperty("recurrence")]
        public string RecurrenceString { get; set; } = "None";

        [Column("completion_count")]
        public int CompletionCount { get; set; } = 0;

        [JsonIgnore]
        public PriorityLevel Priority
        {
            get => Enum.TryParse<PriorityLevel>(PriorityString, out var result) ? result : PriorityLevel.Medium;
            set => PriorityString = value.ToString();
        }

        [JsonIgnore]
        public RecurrenceType Recurrence
        {
            get => Enum.TryParse<RecurrenceType>(RecurrenceString, out var result) ? result : RecurrenceType.None;
            set => RecurrenceString = value.ToString();
        }

        [JsonIgnore]
        public bool IsRecurring => Recurrence != RecurrenceType.None;

    }

    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }
}

