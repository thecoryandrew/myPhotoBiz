using System;
using System.Collections.Generic;

namespace MyPhotoBiz.Models
{
    public class Gallery
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }

        // REMOVED: ClientCode and ClientPassword - now using GalleryAccess for user-based authentication
        // Access is controlled via GalleryAccess records linked to ClientProfile

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string BrandColor { get; set; } = "#2c3e50";
        public string? LogoPath { get; set; }

        // Navigation properties
        public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
        public virtual ICollection<GallerySession> Sessions { get; set; } = new List<GallerySession>();

        // NEW: Access control via authenticated users
        public virtual ICollection<GalleryAccess> Accesses { get; set; } = new List<GalleryAccess>();
    }
}