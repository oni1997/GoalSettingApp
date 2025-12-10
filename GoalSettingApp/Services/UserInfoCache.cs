using System.Collections.Concurrent;

namespace GoalSettingApp.Services
{
    /// <summary>
    /// Caches user information to avoid repeated API calls to Supabase Auth
    /// </summary>
    public class UserInfoCache
    {
        private readonly ConcurrentDictionary<string, CachedUserInfo> _cache = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
        private readonly ILogger<UserInfoCache> _logger;

        public UserInfoCache(ILogger<UserInfoCache> logger)
        {
            _logger = logger;
        }

        public (string? email, string? name)? Get(string userId)
        {
            if (_cache.TryGetValue(userId, out var cached))
            {
                if (DateTime.UtcNow - cached.CachedAt < _cacheExpiration)
                {
                    _logger.LogDebug("Cache hit for user {UserId}", userId);
                    return (cached.Email, cached.Name);
                }

                // Remove expired entry
                _cache.TryRemove(userId, out _);
                _logger.LogDebug("Cache expired for user {UserId}", userId);
            }

            return null;
        }

        public void Set(string userId, string? email, string? name)
        {
            var cached = new CachedUserInfo
            {
                UserId = userId,
                Email = email,
                Name = name,
                CachedAt = DateTime.UtcNow
            };

            _cache[userId] = cached;
            _logger.LogDebug("Cached user info for {UserId}", userId);
        }

        public void Clear()
        {
            _cache.Clear();
            _logger.LogInformation("User cache cleared");
        }

        /// <summary>
        /// Removes expired entries from cache
        /// </summary>
        public int CleanExpired()
        {
            var expiredKeys = _cache
                .Where(kvp => DateTime.UtcNow - kvp.Value.CachedAt >= _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            int removed = 0;
            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out _))
                {
                    removed++;
                }
            }

            if (removed > 0)
            {
                _logger.LogInformation("Cleaned {Count} expired user cache entries", removed);
            }

            return removed;
        }

        private class CachedUserInfo
        {
            public string UserId { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Name { get; set; }
            public DateTime CachedAt { get; set; }
        }
    }
}
