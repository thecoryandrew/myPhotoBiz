using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // Created, Updated, Deleted, Viewed

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // Client, PhotoShoot, Invoice, Contract, Gallery, Album, Photo

        public int? EntityId { get; set; }

        [MaxLength(200)]
        public string? EntityName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // User who performed the action
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Icon helper based on action type
        [NotMapped]
        public string ActionIcon => ActionType switch
        {
            "Created" => "ti ti-plus text-success",
            "Updated" => "ti ti-edit text-warning",
            "Deleted" => "ti ti-trash text-danger",
            "Viewed" => "ti ti-eye text-info",
            "Uploaded" => "ti ti-upload text-primary",
            "Downloaded" => "ti ti-download text-secondary",
            "Signed" => "ti ti-signature text-success",
            "Sent" => "ti ti-send text-primary",
            "Paid" => "ti ti-cash text-success",
            _ => "ti ti-activity text-muted"
        };

        // Entity icon helper
        [NotMapped]
        public string EntityIcon => EntityType switch
        {
            "Client" => "ti ti-user",
            "PhotoShoot" => "ti ti-camera",
            "Invoice" => "ti ti-file-invoice",
            "Contract" => "ti ti-file-text",
            "Gallery" => "ti ti-photo",
            "Album" => "ti ti-folder",
            "Photo" => "ti ti-photo",
            "User" => "ti ti-users",
            _ => "ti ti-file"
        };

        // Background color class based on entity type
        [NotMapped]
        public string EntityBgClass => EntityType switch
        {
            "Client" => "bg-primary-subtle",
            "PhotoShoot" => "bg-warning-subtle",
            "Invoice" => "bg-danger-subtle",
            "Contract" => "bg-info-subtle",
            "Gallery" => "bg-success-subtle",
            "Album" => "bg-success-subtle",
            "Photo" => "bg-success-subtle",
            "User" => "bg-secondary-subtle",
            _ => "bg-light"
        };
    }
}
