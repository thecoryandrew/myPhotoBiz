using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    // TODO: [MEDIUM] Add caching for dashboard stats (Redis or in-memory)
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
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhotoShoot>> GetUpcomingPhotoShootsAsync(int count = 5)
        {
            return await _context.PhotoShoots
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
                .Where(c => !c.IsDeleted)
                .Include(c => c.User)
                .OrderByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();

            // Pending bookings requiring action
            var pendingBookingsCount = await _context.BookingRequests
                .CountAsync(br => br.Status == BookingStatus.Pending);

            // Contracts awaiting signature
            var contractsAwaitingSignature = await _context.Contracts
                .CountAsync(c => c.Status == ContractStatus.PendingSignature);

            // Overdue invoices with aging
            var today = DateTime.Today;
            var overdueInvoices = await _context.Invoices
                .Where(i => i.DueDate < today && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                .Select(i => i.Amount + i.Tax)
                .ToListAsync();
            var overdueInvoicesCount = overdueInvoices.Count;
            var overdueInvoicesAmount = overdueInvoices.Sum();

            // Today's schedule
            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);
            var todaysShoots = await _context.PhotoShoots
                .Include(p => p.ClientProfile)
                    .ThenInclude(cp => cp.User)
                .Where(p => p.ScheduledDate >= todayStart && p.ScheduledDate < todayEnd)
                .OrderBy(p => p.ScheduledDate)
                .ToListAsync();

            // Galleries expiring soon (within 7 days)
            var galleriesExpiringSoonCount = await _context.Galleries
                .CountAsync(g => g.IsActive &&
                    g.ExpiryDate > DateTime.UtcNow &&
                    g.ExpiryDate <= DateTime.UtcNow.AddDays(7));

            // Calculate monthly revenue (current month only)
            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var currentMonthValues = await _context.Invoices
                .Where(inv => inv.InvoiceDate >= currentMonthStart &&
                              inv.Status == InvoiceStatus.Paid)
                .Select(inv => inv.Amount + inv.Tax)
                .ToListAsync();
            var monthlyRevenueCurrent = currentMonthValues.Sum();

            // Calculate yearly revenue (current year)
            var currentYearStart = new DateTime(DateTime.Now.Year, 1, 1);
            var yearlyValues = await _context.Invoices
                .Where(inv => inv.InvoiceDate >= currentYearStart &&
                              inv.Status == InvoiceStatus.Paid)
                .Select(inv => inv.Amount + inv.Tax)
                .ToListAsync();
            var yearlyRevenue = yearlyValues.Sum();

            // Calculate monthly revenue chart data (last 12 months)
            var monthlyRevenueData = new Dictionary<string, decimal>();
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

                monthlyRevenueData[monthKey] = monthValues.Sum();
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
                DurationMinutes = ps.DurationMinutes,
                ClientProfile = ps.ClientProfile
            }).ToList();

            return new DashboardViewModel
            {
                TotalClients = totalClients,
                UpcomingPhotoshoots = pendingPhotoShoots,
                CompletedPhotoshoots = completedPhotoShoots,
                MonthlyRevenue = monthlyRevenueCurrent,
                YearlyRevenue = yearlyRevenue,
                PendingInvoiceAmount = outstandingInvoices,
                PendingBookingsCount = pendingBookingsCount,
                ContractsAwaitingSignature = contractsAwaitingSignature,
                OverdueInvoicesCount = overdueInvoicesCount,
                OverdueInvoicesAmount = overdueInvoicesAmount,
                TodayShootsCount = todaysShoots.Count,
                TodaysShoots = todaysShoots,
                GalleriesExpiringSoonCount = galleriesExpiringSoonCount,
                RecentPhotoshoots = recentPhotoshoots,
                RecentInvoices = recentInvoices.ToList(),
                RecentClients = recentClients,
                MonthlyRevenueData = monthlyRevenueData,
                PhotoshootStatusData = photoshootStatusData
            };
        }
    }
}