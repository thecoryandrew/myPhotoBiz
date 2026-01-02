using Microsoft.AspNetCore.Identity;
using MyPhotoBiz.Models;
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.ViewModels
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
        public IEnumerable<PhotoShootViewModel> RecentPhotoshoots { get; set; } = new List<PhotoShootViewModel>();
        public IEnumerable<Invoice> RecentInvoices { get; set; } = new List<Invoice>();
        public IEnumerable<ClientProfile> RecentClients { get; set; } = new List<ClientProfile>();

        // Chart data
        public IDictionary<string, decimal> MonthlyRevenueData { get; set; } = new Dictionary<string, decimal>();
        public IDictionary<string, int> PhotoshootStatusData { get; set; } = new Dictionary<string, int>();

    }
}