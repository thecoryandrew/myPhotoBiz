using MyPhotoBiz.Models;

namespace MyPhotoBiz.Models.ViewModels
{

    public class ClientDashboardViewModel
    {
        public string ClientName { get; set; } = string.Empty;
        public List<PhotoShoot> UpcomingPhotoshoots { get; set; } = new();
        public List<PhotoShoot> CompletedPhotoshoots { get; set; } = new();
        public List<Album> AccessibleAlbums { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
    }
}