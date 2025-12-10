using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace GoalSettingApp.Models
{
    [Table("user_settings")]
    public class UserSettings : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("preferred_city")]
        public string PreferredCity { get; set; } = "Cape Town";

        [Column("preferred_city_lat")]
        public double PreferredCityLat { get; set; } = -33.9249;

        [Column("preferred_city_lon")]
        public double PreferredCityLon { get; set; } = 18.4241;

        [Column("preferred_country")]
        public string PreferredCountry { get; set; } = "ZA";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    // Model for city search results from OpenWeatherMap Geocoding API
    public class GeocodingResult
    {
        public string Name { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Country { get; set; } = string.Empty;
        public string? State { get; set; }

        public string DisplayName => string.IsNullOrEmpty(State) 
            ? $"{Name}, {Country}" 
            : $"{Name}, {State}, {Country}";
    }
}