using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.ViewModels
{
    public class ServicePackageViewModel
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

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Base Price")]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Display(Name = "Discounted Price")]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal? DiscountedPrice { get; set; }

        [Display(Name = "Duration (Hours)")]
        [Range(0.5, 24)]
        public decimal DurationHours { get; set; } = 2;

        [Display(Name = "Included Photos")]
        [Range(0, 10000)]
        public int IncludedPhotos { get; set; } = 50;

        [Display(Name = "Includes Prints")]
        public bool IncludesPrints { get; set; }

        [Display(Name = "Number of Prints")]
        [Range(0, 1000)]
        public int? NumberOfPrints { get; set; }

        [Display(Name = "Includes Album")]
        public bool IncludesAlbum { get; set; }

        [Display(Name = "Includes Digital Gallery")]
        public bool IncludesDigitalGallery { get; set; } = true;

        [Display(Name = "Number of Locations")]
        [Range(1, 10)]
        public int NumberOfLocations { get; set; } = 1;

        [Display(Name = "Outfit Changes")]
        [Range(0, 20)]
        public int? OutfitChanges { get; set; }

        [StringLength(2000)]
        [Display(Name = "Included Features (comma-separated)")]
        public string? IncludedFeatures { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; }

        [StringLength(500)]
        [Display(Name = "Cover Image Path")]
        public string? CoverImagePath { get; set; }
    }

    public class PackageAddOnViewModel
    {
        public int Id { get; set; }

        public int? ServicePackageId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Display(Name = "Standalone (available for any booking)")]
        public bool IsStandalone { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }

    public class PackageListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal EffectivePrice { get; set; }
        public bool HasDiscount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal DurationHours { get; set; }
        public int IncludedPhotos { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public List<string> Features { get; set; } = new();
        public int AddOnCount { get; set; }
        public int BookingCount { get; set; }
    }
}
