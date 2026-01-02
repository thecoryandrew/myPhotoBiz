using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    /// <summary>
    /// Represents an optional add-on service for a package
    /// </summary>
    public class PackageAddOn
    {
        public int Id { get; set; }

        // Parent package
        public int? ServicePackageId { get; set; }
        public virtual ServicePackage? ServicePackage { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Add-on category
        [StringLength(50)]
        public string? Category { get; set; } // Prints, Albums, Time, Location, etc.

        // If this add-on is standalone (available for any booking)
        public bool IsStandalone { get; set; } = false;

        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
