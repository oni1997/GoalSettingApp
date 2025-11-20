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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [JsonIgnore]
        public PriorityLevel Priority
        {
            get => Enum.TryParse<PriorityLevel>(PriorityString, out var result) ? result : PriorityLevel.Medium;
            set => PriorityString = value.ToString();
        }

    }

    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }
}

