using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClientProfile>> GetAllClientsAsync()
        {
            return await _context.ClientProfiles
                .AsNoTracking()
                .Include(c => c.Invoices)
                .Include(c => c.User)
                .Include(c => c.ClientBadges)
                    .ThenInclude(cb => cb.Badge)
                .OrderBy(c => c.User!.LastName)
                .ThenBy(c => c.User!.FirstName)
                .ToListAsync();
        }

        public async Task<ClientProfile?> GetClientByIdAsync(int id)
        {
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

        public async Task<ClientProfile?> GetClientByUserIdAsync(string userId)
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

        public async Task<ClientProfile> CreateClientAsync(ClientProfile clientProfile)
        {
            _context.ClientProfiles.Add(clientProfile);
            await _context.SaveChangesAsync();
            return clientProfile;
        }

        public async Task<ClientProfile> UpdateClientAsync(ClientProfile clientProfile)
        {
            var existing = await _context.ClientProfiles.FindAsync(clientProfile.Id)
                ?? throw new InvalidOperationException("Client profile not found");

            existing.PhoneNumber = clientProfile.PhoneNumber;
            existing.Address = clientProfile.Address;
            existing.Notes = clientProfile.Notes;
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var clientProfile = await _context.ClientProfiles.FindAsync(id);
            if (clientProfile == null) return false;

            _context.ClientProfiles.Remove(clientProfile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ClientProfile>> SearchClientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllClientsAsync();

            return await _context.ClientProfiles
                .Include(c => c.Invoices)
                .Include(c => c.User)
                .Where(c => c.User!.FirstName.Contains(searchTerm) ||
                            c.User.LastName.Contains(searchTerm) ||
                            c.User.Email!.Contains(searchTerm))
                .OrderBy(c => c.User!.LastName)
                .ThenBy(c => c.User!.FirstName)
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
