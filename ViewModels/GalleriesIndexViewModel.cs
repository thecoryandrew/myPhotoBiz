using System;
using System.Collections.Generic;

namespace MyPhotoBiz.ViewModels
{
    public class GalleriesIndexViewModel
    {
        public List<GalleryListItemViewModel> Galleries { get; set; } = new();
        public GalleryStatsSummaryViewModel Stats { get; set; } = new();
    }

    public class GalleryListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;
        public string Status
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (IsExpired) return "Expired";
                return "Active";
            }
        }

        // Analytics
        public int PhotoCount { get; set; }
        public int SessionCount { get; set; }
        public int TotalProofs { get; set; }
        public DateTime? LastAccessDate { get; set; }
    }

    public class GalleryStatsSummaryViewModel
    {
        public int TotalGalleries { get; set; }
        public int ActiveGalleries { get; set; }
        public int ExpiredGalleries { get; set; }
        public int TotalSessions { get; set; }
        public int TotalPhotos { get; set; }
    }
}
