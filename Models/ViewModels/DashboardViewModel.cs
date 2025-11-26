using MyPhotoBiz.Models;

namespace MyPhotoBiz.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Counts
        public int TotalClients { get; set; } = 0;
        public int UpcomingPhotoshoots { get; set; } = 0;
        public int CompletedPhotoshoots { get; set; } = 0;
        public int TotalAlbums { get; set; } = 0;
        public int TotalPhotos { get; set; } = 0;

        // Financials
        public decimal MonthlyRevenue { get; set; } = 0m;
        public decimal YearlyRevenue { get; set; } = 0m;
        public decimal PendingInvoiceAmount { get; set; } = 0m;

        // Recent activity
        public IEnumerable<PhotoShoot> RecentPhotoshoots { get; set; } = new List<PhotoShoot>();
        public IEnumerable<Invoice> RecentInvoices { get; set; } = new List<Invoice>();
        public IEnumerable<Client> RecentClients { get; set; } = new List<Client>();

        // Chart data
        public IDictionary<string, decimal> MonthlyRevenueData { get; set; } = new Dictionary<string, decimal>();
        public IDictionary<string, int> PhotoshootStatusData { get; set; } = new Dictionary<string, int>();
    }
}