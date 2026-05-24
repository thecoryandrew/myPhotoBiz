using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IBadgeService
    {
        Task<IEnumerable<Badge>> GetAllBadgesAsync();
        Task<Badge?> GetBadgeByIdAsync(int id);
        Task<Badge> CreateBadgeAsync(Badge badge);
        Task UpdateBadgeAsync(Badge badge);
        Task<bool> DeleteBadgeAsync(int id);

        Task<int> SeedDefaultBadgesAsync();
        Task<int> AwardNewUserBadgeToExistingClientsAsync();

        Task<bool> AwardBadgeByNameAsync(int clientProfileId, string badgeName, string? notes = null);
        Task<bool> AwardBadgeAsync(int clientProfileId, int badgeId, int? contractId = null, string? notes = null);
    }
}
