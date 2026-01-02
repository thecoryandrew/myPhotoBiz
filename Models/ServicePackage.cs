using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    /// <summary>
    /// Represents a photography service package (e.g., Wedding Package, Portrait Package)
    /// </summary>
    public class ServicePackage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(2000)]
        [Display(Name = "Detailed Description")]
        public string? DetailedDescription { get; set; }

        // Package category
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // Wedding, Portrait, Event, Commercial, etc.

        // Pricing
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Price")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discounted Price")]
        public decimal? DiscountedPrice { get; set; }

        // Duration
        [Display(Name = "Duration (Hours)")]
        [Range(0.5, 24)]
        public decimal DurationHours { get; set; } = 2;

        // Included items
        [Display(Name = "Included Photos")]
        public int IncludedPhotos { get; set; } = 50;

        [Display(Name = "Includes Prints")]
        public bool IncludesPrints { get; set; } = false;

        [Display(Name = "Number of Prints")]
        public int? NumberOfPrints { get; set; }

        [Display(Name = "Includes Album")]
        public bool IncludesAlbum { get; set; } = false;

        [Display(Name = "Includes Digital Gallery")]
        public bool IncludesDigitalGallery { get; set; } = true;

        [Display(Name = "Number of Locations")]
        public int NumberOfLocations { get; set; } = 1;

        [Display(Name = "Number of Outfit Changes")]
        public int? OutfitChanges { get; set; }

        // Features as JSON or comma-separated list
        [StringLength(2000)]
        [Display(Name = "Included Features")]
        public string? IncludedFeatures { get; set; }

        // Display settings
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        // Image for package display
        [StringLength(500)]
        [Display(Name = "Cover Image")]
        public string? CoverImagePath { get; set; }

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PackageAddOn> AddOns { get; set; } = new List<PackageAddOn>();
        public virtual ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();

        // Computed properties
        [NotMapped]
        public decimal EffectivePrice => DiscountedPrice ?? BasePrice;

        [NotMapped]
        public bool HasDiscount => DiscountedPrice.HasValue && DiscountedPrice < BasePrice;

        [NotMapped]
        public decimal? DiscountPercentage => HasDiscount
            ? Math.Round((1 - (DiscountedPrice!.Value / BasePrice)) * 100, 0)
            : null;

        [NotMapped]
        public List<string> FeaturesList => string.IsNullOrEmpty(IncludedFeatures)
            ? new List<string>()
            : IncludedFeatures.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList();
    }
}
