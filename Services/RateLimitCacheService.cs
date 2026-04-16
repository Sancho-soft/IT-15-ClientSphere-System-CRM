namespace ClientSphere.Services
{
    public class RateLimitCacheService
    {
        /// <summary>
        /// Stores the currently configured API rate limit in memory to avoid blocking database calls during middleware execution.
        /// Defaults to 100 on startup until initialized from the database.
        /// </summary>
        public int CurrentApiRateLimit { get; set; } = 100;
    }
}
