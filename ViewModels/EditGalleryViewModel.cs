using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.ViewModels
{
    public class EditGalleryViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Must be a valid hex color")]
        [Display(Name = "Brand Color")]
        public string BrandColor { get; set; } = "#2c3e50";

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        // Album management
        public List<int> SelectedAlbumIds { get; set; } = new();
        public List<AlbumSelectionViewModel> AvailableAlbums { get; set; } = new();

        // Client access management
        public List<int> SelectedClientProfileIds { get; set; } = new();
        public List<ClientSelectionViewModel> AvailableClients { get; set; } = new();
    }
}
