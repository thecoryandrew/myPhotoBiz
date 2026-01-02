using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;

        public ActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string actionType, string entityType, int? entityId = null,
            string? entityName = null, string? description = null, string? userId = null)
        {
            var activity = new Activity
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description ?? GenerateDefaultDescription(actionType, entityType, entityName),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Activity>> GetRecentActivitiesAsync(int count = 10)
        {
            return await _context.Activities
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Activity>> GetActivitiesByEntityTypeAsync(string entityType, int count = 50)
        {
            return await _context.Activities
                .Include(a => a.User)
                .Where(a => a.EntityType == entityType)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Activity>> GetActivitiesForEntityAsync(string entityType, int entityId)
        {
            return await _context.Activities
                .Include(a => a.User)
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Activity>> GetActivitiesByUserAsync(string userId, int count = 50)
        {
            return await _context.Activities
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<ActivityStats> GetActivityStatsAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var activities = await _context.Activities
                .Where(a => a.CreatedAt >= startDate)
                .ToListAsync();

            var stats = new ActivityStats
            {
                TotalActivities = activities.Count,
                CreatedCount = activities.Count(a => a.ActionType == "Created"),
                UpdatedCount = activities.Count(a => a.ActionType == "Updated"),
                DeletedCount = activities.Count(a => a.ActionType == "Deleted"),
                ActivitiesByEntityType = activities
                    .GroupBy(a => a.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ActivitiesByDay = activities
                    .GroupBy(a => a.CreatedAt.Date.ToString("MMM dd"))
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }

        public async Task CleanupOldActivitiesAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

            var oldActivities = await _context.Activities
                .Where(a => a.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.Activities.RemoveRange(oldActivities);
            await _context.SaveChangesAsync();
        }

        private string GenerateDefaultDescription(string actionType, string entityType, string? entityName)
        {
            var name = !string.IsNullOrEmpty(entityName) ? $" \"{entityName}\"" : "";
            return $"{actionType} {entityType.ToLower()}{name}";
        }
    }
}
