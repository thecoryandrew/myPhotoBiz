
// Models/Client.cs
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string FullName => $"{FirstName} {LastName}";
        // Foreign key
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // Navigation properties
        public virtual ICollection<PhotoShoot> PhotoShoots { get; set; } = new List<PhotoShoot>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}