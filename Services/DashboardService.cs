
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Models;
using MyPhotoBiz.Models.ViewModels;

namespace MyPhotoBiz.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> GetClientsCountAsync() =>
            await _context.Clients.CountAsync();

        public async Task<int> GetPhotoShootsCountAsync() =>
            await _context.PhotoShoots.CountAsync();

        public async Task<int> GetPendingPhotoShootsCountAsync() =>
            await _context.PhotoShoots
                .Where(p => p.Status == PhotoShootStatus.Scheduled)
                .CountAsync();

        public async Task<decimal> GetTotalRevenueAsync()
        {
            var amounts = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Select(i => i.Amount + i.Tax)
                .ToListAsync();

            return amounts.Sum();
        }

        public async Task<decimal> GetOutstandingInvoicesAsync()
        {
            var amounts = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue)
                .Select(i => i.Amount + i.Tax)
                .ToListAsync();

            return amounts.Sum();
        }

        public async Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count = 5)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.PhotoShoot)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhotoShoot>> GetUpcomingPhotoShootsAsync(int count = 5)
        {
            return await _context.PhotoShoots
                .Include(p => p.Client)
                .Where(p => p.ScheduledDate >= DateTime.Today)
                .OrderBy(p => p.ScheduledDate)
                .Take(count)
                .ToListAsync();
        }

        // Added missing method implementation
        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var totalClients = await GetClientsCountAsync();
            var totalPhotoShoots = await GetPhotoShootsCountAsync();
            var pendingPhotoShoots = await GetPendingPhotoShootsCountAsync();
            var completedPhotoShoots = await _context.PhotoShoots.CountAsync(p => p.Status == PhotoShootStatus.Completed);

            var totalRevenue = await GetTotalRevenueAsync();
            var outstandingInvoices = await GetOutstandingInvoicesAsync();

            var recentPhotoShoots = await GetUpcomingPhotoShootsAsync(5);
            var recentInvoices = await GetRecentInvoicesAsync(5);

            // Calculate monthly revenue (simplified - last 12 months)
            var monthlyRevenue = new Dictionary<string, decimal>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthKey = month.ToString("MMM yyyy");
                // Sum on client side because SQLite provider doesn't support SUM on decimal expressions
                var monthValues = await _context.Invoices
                    .Where(inv => inv.InvoiceDate.Month == month.Month &&
                                  inv.InvoiceDate.Year == month.Year &&
                                  inv.Status == InvoiceStatus.Paid)
                    .Select(inv => inv.Amount + inv.Tax)
                    .ToListAsync();
                var monthlyTotal = monthValues.Sum();
                monthlyRevenue[monthKey] = monthlyTotal;
            }

            return new DashboardViewModel
            {
                TotalClients = totalClients,
                UpcomingPhotoshoots = pendingPhotoShoots,
                CompletedPhotoshoots = completedPhotoShoots,
                MonthlyRevenue = totalRevenue,
                YearlyRevenue = totalRevenue, // Simplified
                PendingInvoiceAmount = outstandingInvoices,
                RecentPhotoshoots = recentPhotoShoots,
                RecentInvoices = recentInvoices,
                MonthlyRevenueData = monthlyRevenue
            };
        }
    }
}
