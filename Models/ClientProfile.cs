using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class ClientProfile
    {
        public int Id { get; set; }

        // 1:1 relationship with ApplicationUser
        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Business-specific data (migrated from Client)
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties (relationships migrated from Client)
        public virtual ICollection<PhotoShoot> PhotoShoots { get; set; } = new List<PhotoShoot>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<ClientBadge> ClientBadges { get; set; } = new List<ClientBadge>();
        public virtual ICollection<GalleryAccess> GalleryAccesses { get; set; } = new List<GalleryAccess>();

        // Computed property for convenience (gets from ApplicationUser)
        public string FullName => User != null ? $"{User.FirstName} {User.LastName}" : string.Empty;
        public string Email => User?.Email ?? string.Empty;
    }
}
