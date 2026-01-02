
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    // TODO: [HIGH] Implement soft delete - currently permanent delete loses all client data
    // TODO: [HIGH] Add validation before delete (check for active bookings, unpaid invoices)
    // TODO: [HIGH] Add client status field (Active, Inactive, Archived)
    // TODO: [MEDIUM] Add client categorization/segmentation (VIP, Regular, Prospect)
    // TODO: [MEDIUM] Add search filters by date range, spend amount, activity status
    // TODO: [MEDIUM] Add duplicate client detection
    // TODO: [FEATURE] Add client self-service portal with password reset
    // TODO: [FEATURE] Add client lifetime value calculation
    // TODO: [FEATURE] Add referral tracking (how they found you)
    // TODO: [FEATURE] Add client preferences (contact method, delivery preferences)
    // TODO: [FEATURE] Add client communication history timeline
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientService> _logger;

        public ClientService(ApplicationDbContext context, ILogger<ClientService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ClientProfile>> GetAllClientsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all clients");
                return await _context.ClientProfiles
                    .AsNoTracking()
                    .Include(c => c.Invoices)
                    .Include(c => c.User)
                    .Include(c => c.ClientBadges)
                        .ThenInclude(cb => cb.Badge)
                    .OrderBy(c => c.User.LastName)
                    .ThenBy(c => c.User.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all clients");
                throw;
            }
        }

        public async Task<ClientProfile?> GetClientByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving client with ID: {ClientId}", id);
                return await _context.ClientProfiles
                    .Include(c => c.Invoices)
                    .Include(c => c.PhotoShoots)
                    .Include(c => c.User)
                    .Include(c => c.ClientBadges)
                        .ThenInclude(cb => cb.Badge)
                    .Include(c => c.GalleryAccesses)
                        .ThenInclude(ga => ga.Gallery)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client with ID: {ClientId}", id);
                throw;
            }
        }

        public async Task<ClientProfile?> GetClientByUserIdAsync(string userId)
        {
            try
            {
                return await _context.ClientProfiles
                    .Include(c => c.Invoices)
                    .Include(c => c.PhotoShoots)
                    .Include(c => c.User)
                    .Include(c => c.ClientBadges)
                        .ThenInclude(cb => cb.Badge)
                    .Include(c => c.GalleryAccesses)
                        .ThenInclude(ga => ga.Gallery)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client by user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<ClientProfile> CreateClientAsync(ClientProfile clientProfile)
        {
            if (clientProfile == null) throw new ArgumentNullException(nameof(clientProfile));

            try
            {
                _logger.LogInformation("Creating new client profile for user: {UserId}", clientProfile.UserId);
                _context.ClientProfiles.Add(clientProfile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created client profile with ID: {ClientId}", clientProfile.Id);
                return clientProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client profile for user: {UserId}", clientProfile.UserId);
                throw;
            }
        }

        public async Task<ClientProfile> UpdateClientAsync(ClientProfile clientProfile)
        {
            if (clientProfile == null) throw new ArgumentNullException(nameof(clientProfile));

            try
            {
                _logger.LogInformation("Updating client profile with ID: {ClientId}", clientProfile.Id);
                var existing = await _context.ClientProfiles.FindAsync(clientProfile.Id);
                if (existing == null)
                {
                    _logger.LogWarning("Attempted to update non-existent client profile with ID: {ClientId}", clientProfile.Id);
                    throw new InvalidOperationException("Client profile not found");
                }

                existing.PhoneNumber = clientProfile.PhoneNumber;
                existing.Address = clientProfile.Address;
                existing.Notes = clientProfile.Notes;
                existing.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated client profile with ID: {ClientId}", clientProfile.Id);
                return existing;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error updating client profile with ID: {ClientId}", clientProfile.Id);
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting client profile with ID: {ClientId}", id);
                var clientProfile = await _context.ClientProfiles.FindAsync(id);
                if (clientProfile == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent client profile with ID: {ClientId}", id);
                    return false;
                }

                _context.ClientProfiles.Remove(clientProfile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted client profile with ID: {ClientId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client profile with ID: {ClientId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ClientProfile>> SearchClientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllClientsAsync();

            return await _context.ClientProfiles
                .Include(c => c.Invoices)
                .Include(c => c.User)
                .Where(c => c.User.FirstName.Contains(searchTerm) ||
                           c.User.LastName.Contains(searchTerm) ||
                           c.User.Email!.Contains(searchTerm))
                .OrderBy(c => c.User.LastName)
                .ThenBy(c => c.User.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<GalleryAccess>> GetClientGalleryAccessesAsync(int clientProfileId)
        {
            return await _context.GalleryAccesses
                .Include(ga => ga.Gallery)
                .Where(ga => ga.ClientProfileId == clientProfileId && ga.IsActive)
                .OrderByDescending(ga => ga.GrantedDate)
                .ToListAsync();
        }
    }
}
