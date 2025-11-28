
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientService> _logger;

        public ClientService(ApplicationDbContext context, ILogger<ClientService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all clients");
                return await _context.Clients
                    .AsNoTracking()
                    .Include(c => c.Invoices)
                    .Include(c => c.User)
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all clients");
                throw;
            }
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving client with ID: {ClientId}", id);
                return await _context.Clients
                    .Include(c => c.Invoices)
                    .Include(c => c.PhotoShoots)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client with ID: {ClientId}", id);
                throw;
            }
        }

        // Added missing method implementation
        public async Task<Client?> GetClientByUserIdAsync(string userId) =>
            await _context.Clients
                .Include(c => c.Invoices)
                .Include(c => c.PhotoShoots)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Client> CreateClientAsync(Client client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            try
            {
                _logger.LogInformation("Creating new client: {ClientEmail}", client.Email);
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created client with ID: {ClientId}", client.Id);
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client: {ClientEmail}", client.Email);
                throw;
            }
        }

        public async Task<Client> UpdateClientAsync(Client client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            try
            {
                _logger.LogInformation("Updating client with ID: {ClientId}", client.Id);
                var existing = await _context.Clients.FindAsync(client.Id);
                if (existing == null)
                {
                    _logger.LogWarning("Attempted to update non-existent client with ID: {ClientId}", client.Id);
                    throw new InvalidOperationException("Client not found");
                }

                _context.Entry(existing).CurrentValues.SetValues(client);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated client with ID: {ClientId}", client.Id);
                return existing;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error updating client with ID: {ClientId}", client.Id);
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting client with ID: {ClientId}", id);
                var client = await _context.Clients.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent client with ID: {ClientId}", id);
                    return false;
                }

                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted client with ID: {ClientId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client with ID: {ClientId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllClientsAsync();

            return await _context.Clients
                .Include(c => c.Invoices)
                .Include(c => c.User)
                .Where(c => c.FirstName.Contains(searchTerm) || c.LastName.Contains(searchTerm) || c.Email.Contains(searchTerm))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
        }
    }
}