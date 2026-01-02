using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class GallerySession
    {
        public int Id { get; set; }
        public int GalleryId { get; set; }
        public required string SessionToken { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessDate { get; set; } = DateTime.UtcNow;

        // Link to authenticated user
        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Proof>? Proofs { get; set; }
        public virtual Gallery Gallery { get; set; } = null!;
    }
}