
// Fixed Services/IDashboardService.cs
using MyPhotoBiz.Models;
using MyPhotoBiz.Models.ViewModels;

namespace MyPhotoBiz.Services
{
    public interface IDashboardService
    {
        Task<int> GetClientsCountAsync();
        Task<int> GetPhotoShootsCountAsync();
        Task<int> GetPendingPhotoShootsCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetOutstandingInvoicesAsync();
        Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count = 5);
        Task<IEnumerable<PhotoShoot>> GetUpcomingPhotoShootsAsync(int count = 5);
        Task<DashboardViewModel> GetDashboardDataAsync(); // Added missing method
    }
}
