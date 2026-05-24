using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Badge>> GetAllBadgesAsync()
        {
            return await _context.Badges
                .Include(b => b.ClientBadges)
                .OrderBy(b => b.Name)
                .ToListAsync();
        }

        public async Task<Badge?> GetBadgeByIdAsync(int id)
        {
            return await _context.Badges.FindAsync(id);
        }

        public async Task<Badge> CreateBadgeAsync(Badge badge)
        {
            badge.CreatedDate = DateTime.UtcNow;
            _context.Badges.Add(badge);
            await _context.SaveChangesAsync();
            return badge;
        }

        public async Task UpdateBadgeAsync(Badge badge)
        {
            _context.Badges.Update(badge);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteBadgeAsync(int id)
        {
            var badge = await _context.Badges.FindAsync(id);
            if (badge == null) return false;

            _context.Badges.Remove(badge);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> SeedDefaultBadgesAsync()
        {
            var defaultBadges = new List<Badge>
            {
                new() { Name = "New User", Description = "Welcome to myPhotoBiz! Account created successfully", Type = BadgeType.Custom, Icon = "ti-user-plus", Color = "#007bff", IsActive = true },
                new() { Name = "Contract Signed", Description = "Completed and signed a contract", Type = BadgeType.ContractSigned, Icon = "ti-file-check", Color = "#28a745", IsActive = true },
                new() { Name = "Waiver Signed", Description = "Signed liability waiver", Type = BadgeType.WaiverSigned, Icon = "ti-shield-check", Color = "#17a2b8", IsActive = true },
                new() { Name = "Payment Complete", Description = "Made full payment", Type = BadgeType.PaymentCompleted, Icon = "ti-credit-card", Color = "#ffc107", IsActive = true },
                new() { Name = "First Session", Description = "Completed first photo shoot", Type = BadgeType.FirstSession, Icon = "ti-camera", Color = "#6610f2", IsActive = true },
                new() { Name = "Returning Client", Description = "Booked second photo shoot", Type = BadgeType.ReturningClient, Icon = "ti-refresh", Color = "#e83e8c", IsActive = true },
                new() { Name = "VIP Client", Description = "Premium client status", Type = BadgeType.VIPClient, Icon = "ti-star", Color = "#fd7e14", IsActive = true },
                new() { Name = "Referral Source", Description = "Referred new clients", Type = BadgeType.ReferralSource, Icon = "ti-users", Color = "#20c997", IsActive = true }
            };

            var existingNames = await _context.Badges
                .Where(b => defaultBadges.Select(d => d.Name).Contains(b.Name))
                .Select(b => b.Name)
                .ToListAsync();

            var toAdd = defaultBadges.Where(b => !existingNames.Contains(b.Name)).ToList();
            if (toAdd.Count == 0) return 0;

            _context.Badges.AddRange(toAdd);
            await _context.SaveChangesAsync();
            return toAdd.Count;
        }

        public async Task<int> AwardNewUserBadgeToExistingClientsAsync()
        {
            var newUserBadge = await _context.Badges
                .FirstOrDefaultAsync(b => b.Name == "New User");

            if (newUserBadge == null)
                throw new InvalidOperationException("New User badge not found. Please seed default badges first.");

            var clientsWithoutBadge = await _context.ClientProfiles
                .Where(c => !c.ClientBadges.Any(cb => cb.BadgeId == newUserBadge.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (clientsWithoutBadge.Count == 0) return 0;

            foreach (var clientProfileId in clientsWithoutBadge)
            {
                _context.ClientBadges.Add(new ClientBadge
                {
                    ClientProfileId = clientProfileId,
                    BadgeId = newUserBadge.Id,
                    EarnedDate = DateTime.UtcNow,
                    Notes = "Awarded to existing client"
                });
            }

            await _context.SaveChangesAsync();
            return clientsWithoutBadge.Count;
        }

        public async Task<bool> AwardBadgeByNameAsync(int clientProfileId, string badgeName, string? notes = null)
        {
            var badge = await _context.Badges
                .FirstOrDefaultAsync(b => b.Name == badgeName && b.IsActive);

            if (badge == null) return false;

            return await AwardBadgeAsync(clientProfileId, badge.Id, contractId: null, notes);
        }

        public async Task<bool> AwardBadgeAsync(int clientProfileId, int badgeId, int? contractId = null, string? notes = null)
        {
            var alreadyAwarded = await _context.ClientBadges
                .AnyAsync(cb => cb.ClientProfileId == clientProfileId && cb.BadgeId == badgeId);

            if (alreadyAwarded) return false;

            _context.ClientBadges.Add(new ClientBadge
            {
                ClientProfileId = clientProfileId,
                BadgeId = badgeId,
                ContractId = contractId,
                EarnedDate = DateTime.UtcNow,
                Notes = notes
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
