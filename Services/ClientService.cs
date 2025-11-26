
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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync() =>
            await _context.Clients
                .Include(c => c.Invoices)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

        public async Task<Client?> GetClientByIdAsync(int id) =>
            await _context.Clients
                .Include(c => c.Invoices)
                .Include(c => c.PhotoShoots)
                .FirstOrDefaultAsync(c => c.Id == id);

        // Added missing method implementation
        public async Task<Client?> GetClientByUserIdAsync(string userId) =>
            await _context.Clients
                .Include(c => c.Invoices)
                .Include(c => c.PhotoShoots)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Client> CreateClientAsync(Client client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return client;
        }

        public async Task<Client> UpdateClientAsync(Client client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            var existing = await _context.Clients.FindAsync(client.Id);
            if (existing == null) throw new InvalidOperationException("Client not found");

            _context.Entry(existing).CurrentValues.SetValues(client);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return false;

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllClientsAsync();

            return await _context.Clients
                .Include(c => c.Invoices)
                .Where(c => c.FirstName.Contains(searchTerm) || c.LastName.Contains(searchTerm) || c.Email.Contains(searchTerm))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
        }
    }
}