using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.ViewModels
{
    public class CreateGalleryViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Gallery Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(3);

        [Display(Name = "Brand Color")]
        [RegularExpression("^#([A-Fa-f0-9]{6})$", ErrorMessage = "Must be a valid hex color")]
        public string BrandColor { get; set; } = "#2c3e50";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Select Albums")]
        public List<int> SelectedAlbumIds { get; set; } = new();

        [Display(Name = "Grant Access To Clients")]
        public List<int> SelectedClientProfileIds { get; set; } = new();

        public List<AlbumSelectionViewModel> AvailableAlbums { get; set; } = new();
        public List<ClientSelectionViewModel> AvailableClients { get; set; } = new();
    }

    public class AlbumSelectionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PhotoCount { get; set; }
        public string? ClientName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class PhotoSelectionViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? AlbumId { get; set; }
        public string? AlbumName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ClientGalleryViewModel
    {
        public int GalleryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? BrandColor { get; set; }
        public int PhotoCount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime GrantedDate { get; set; }
        public bool CanDownload { get; set; }
        public bool CanProof { get; set; }
        public bool CanOrder { get; set; }
    }
}
