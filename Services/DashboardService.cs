using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    // TODO: [HIGH] Dashboard is missing key metrics:
    //       - Pending bookings requiring action
    //       - Contracts awaiting signature
    //       - Overdue invoices with aging breakdown
    //       - Today's schedule at-a-glance
    //       - Galleries expiring soon
    // TODO: [MEDIUM] Add caching for dashboard stats (Redis or in-memory)
    // TODO: [MEDIUM] MonthlyRevenue calculation should filter by current month, not total
    // TODO: [MEDIUM] YearlyRevenue is same as MonthlyRevenue - implement actual yearly calc
    // TODO: [FEATURE] Add revenue forecast/trend analysis
    // TODO: [FEATURE] Add client acquisition metrics
    // TODO: [FEATURE] Add photographer utilization stats
    // TODO: [FEATURE] Add recent activity timeline
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> GetClientsCountAsync() =>
            await _context.ClientProfiles.CountAsync();

        public async Task<int> GetPhotoShootsCountAsync() =>
            await _context.PhotoShoots.CountAsync();

        public async Task<int> GetPendingPhotoShootsCountAsync() =>
            await _context.PhotoShoots
                .Where(p => p.Status == PhotoShootStatus.Scheduled)
                .CountAsync();

        public async Task<decimal> GetTotalRevenueAsync() =>
            await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .SumAsync(i => i.Amount + i.Tax);

        public async Task<decimal> GetOutstandingInvoicesAsync() =>
            await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => i.Amount + i.Tax);

        public async Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count = 5)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhotoShoot>> GetUpcomingPhotoShootsAsync(int count = 5)
        {
            return await _context.PhotoShoots
                .AsNoTracking()
                .Include(p => p.ClientProfile)
                .Where(p => p.ScheduledDate >= DateTime.Today)
                .OrderBy(p => p.ScheduledDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var totalClients = await GetClientsCountAsync();
            var pendingPhotoShoots = await GetPendingPhotoShootsCountAsync();
            var completedPhotoShoots = await _context.PhotoShoots
                .CountAsync(p => p.Status == PhotoShootStatus.Completed);

            var totalRevenue = await GetTotalRevenueAsync();
            var outstandingInvoices = await GetOutstandingInvoicesAsync();

            var upcomingPhotoShoots = await GetUpcomingPhotoShootsAsync(5);
            var recentInvoices = await GetRecentInvoicesAsync(5);
            var recentClients = await _context.ClientProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .OrderByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();

            // Monthly revenue for the last 12 months, aggregated in a single grouped query.
            var windowStart = new DateTime(DateTime.Now.AddMonths(-11).Year, DateTime.Now.AddMonths(-11).Month, 1);
            var revenueByMonth = await _context.Invoices
                .Where(inv => inv.Status == InvoiceStatus.Paid && inv.InvoiceDate >= windowStart)
                .GroupBy(inv => new { inv.InvoiceDate.Year, inv.InvoiceDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(inv => inv.Amount + inv.Tax) })
                .ToDictionaryAsync(x => (x.Year, x.Month), x => x.Total);

            var monthlyRevenue = new Dictionary<string, decimal>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                revenueByMonth.TryGetValue((month.Year, month.Month), out var total);
                monthlyRevenue[month.ToString("MMM yyyy")] = total;
            }

            // Calculate photoshoot status data for charts
            var photoshootStatusData = new Dictionary<string, int>
            {
                ["Scheduled"] = await _context.PhotoShoots.CountAsync(p => p.Status == PhotoShootStatus.Scheduled),
                ["Completed"] = completedPhotoShoots,
                ["Cancelled"] = await _context.PhotoShoots.CountAsync(p => p.Status == PhotoShootStatus.Cancelled),
                ["InProgress"] = await _context.PhotoShoots.CountAsync(p => p.Status == PhotoShootStatus.InProgress)
            };

            // Convert PhotoShoots to PhotoShootViewModel
            var recentPhotoshoots = upcomingPhotoShoots.Select(ps => new PhotoShootViewModel
            {
                Id = ps.Id,
                Title = ps.Title,
                ClientId = ps.ClientProfileId,
                Location = ps.Location ?? string.Empty,
                ScheduledDate = ps.ScheduledDate,
                UpdatedDate = ps.UpdatedDate,
                Status = ps.Status,
                Price = ps.Price,
                Notes = ps.Notes,
                DurationHours = ps.DurationHours,
                DurationMinutes = ps.DurationMinutes
                ,
                ClientProfile = ps.ClientProfile
            }).ToList();

            return new DashboardViewModel
            {
                TotalClients = totalClients,
                UpcomingPhotoshoots = pendingPhotoShoots,
                CompletedPhotoshoots = completedPhotoShoots,
                MonthlyRevenue = totalRevenue,
                YearlyRevenue = totalRevenue, // You may want to calculate actual yearly revenue
                PendingInvoiceAmount = outstandingInvoices,
                RecentPhotoshoots = recentPhotoshoots,
                RecentInvoices = recentInvoices.ToList(),
                RecentClients = recentClients,
                MonthlyRevenueData = monthlyRevenue,
                PhotoshootStatusData = photoshootStatusData
            };
        }
    }
}