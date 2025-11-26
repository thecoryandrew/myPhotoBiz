using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Models.ViewModels
{
    public class PhotoShootViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        // Dropdown list for selecting client in the view
        public IEnumerable<SelectListItem>? Clients { get; set; }

        [Required]
        [Display(Name = "Scheduled Date")]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [Display(Name = "Updated Date")]
        public DateTime UpdatedDate { get; set; }
        [StringLength(200)]
        public string? Location { get; set; }

        [Required]
        public PhotoShootStatus Status { get; set; }

        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public string? Notes { get; set; }

        public int DurationHours { get; set; }
        public int DurationMinutes { get; set; }
    }


}
