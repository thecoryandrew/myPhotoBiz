using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Title { get; set; }

        [Required]
        public NotificationType Type { get; set; } = NotificationType.Info;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ReadDate { get; set; }

        public string? Link { get; set; }

        public string? Icon { get; set; }

        // Navigation property
        public ApplicationUser? User { get; set; }
    }

    public enum NotificationType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
        Invoice = 4,
        PhotoShoot = 5,
        Client = 6,
        Album = 7
    }
}
