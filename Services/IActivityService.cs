using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IActivityService
    {
        /// <summary>
        /// Log a new activity
        /// </summary>
        Task LogActivityAsync(string actionType, string entityType, int? entityId = null,
            string? entityName = null, string? description = null, string? userId = null);

        /// <summary>
        /// Get recent activities for the dashboard
        /// </summary>
        Task<IEnumerable<Activity>> GetRecentActivitiesAsync(int count = 10);

        /// <summary>
        /// Get activities filtered by entity type
        /// </summary>
        Task<IEnumerable<Activity>> GetActivitiesByEntityTypeAsync(string entityType, int count = 50);

        /// <summary>
        /// Get activities for a specific entity
        /// </summary>
        Task<IEnumerable<Activity>> GetActivitiesForEntityAsync(string entityType, int entityId);

        /// <summary>
        /// Get activities by user
        /// </summary>
        Task<IEnumerable<Activity>> GetActivitiesByUserAsync(string userId, int count = 50);

        /// <summary>
        /// Get activity statistics for the dashboard
        /// </summary>
        Task<ActivityStats> GetActivityStatsAsync(int days = 7);

        /// <summary>
        /// Delete old activities (cleanup)
        /// </summary>
        Task CleanupOldActivitiesAsync(int daysToKeep = 90);
    }

    public class ActivityStats
    {
        public int TotalActivities { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int DeletedCount { get; set; }
        public Dictionary<string, int> ActivitiesByEntityType { get; set; } = new();
        public Dictionary<string, int> ActivitiesByDay { get; set; } = new();
    }
}
