using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class GalleryAccess
    {
        public int Id { get; set; }

        // Gallery this access is for
        public int GalleryId { get; set; }
        public virtual Gallery Gallery { get; set; } = null!;

        // Client who has access
        public int ClientProfileId { get; set; }
        public virtual ClientProfile ClientProfile { get; set; } = null!;

        // Access details
        public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Permission flags
        public bool CanDownload { get; set; } = true;
        public bool CanProof { get; set; } = true;
        public bool CanOrder { get; set; } = true;

        // Computed property to check if access is valid
        public bool IsValid => IsActive && (!ExpiryDate.HasValue || ExpiryDate > DateTime.UtcNow);
    }
}
