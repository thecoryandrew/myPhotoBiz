using System;
using System.Collections.Generic;

namespace MyPhotoBiz.ViewModels
{
    public class GalleryDetailsViewModel
    {
        // Basic Info
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;
        public string BrandColor { get; set; } = "#2c3e50";
        public string Status
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (IsExpired) return "Expired";
                return "Active";
            }
        }

        // Photos
        public int PhotoCount { get; set; }
        public List<PhotoViewModel> Photos { get; set; } = new();

        // Sessions Analytics
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public DateTime? LastAccessDate { get; set; }
        public List<GallerySessionViewModel> RecentSessions { get; set; } = new();

        // Proofs Analytics
        public int TotalProofs { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalEditingRequests { get; set; }

        // Access Info
        public string AccessUrl { get; set; } = string.Empty;
    }

    public class PhotoViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string FullImagePath { get; set; } = string.Empty;
    }

    public class GallerySessionViewModel
    {
        public int Id { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastAccessDate { get; set; }
        public int ProofCount { get; set; }
        public TimeSpan Duration => LastAccessDate - CreatedDate;
    }
}
