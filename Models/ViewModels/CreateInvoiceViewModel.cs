// ViewModels/CreateInvoiceViewModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models.ViewModels
{
    public class CreateInvoiceViewModel
    {
        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        public string? ClientName { get; set; }

        [EmailAddress]
        public string? ClientEmail { get; set; }

        [Display(Name = "Photo Shoot")]
        public int? PhotoShootId { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Display(Name = "Tax")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tax cannot be negative")]
        public decimal Tax { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Changed from 'Items' to 'InvoiceItems' to match the view
        [Display(Name = "Invoice Items")]
        public List<InvoiceItemViewModel> InvoiceItems { get; set; } = new List<InvoiceItemViewModel>();

        [NotMapped]
        public decimal TotalAmount => Amount + Tax;
    }

    public class InvoiceItemViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal Total => Quantity * UnitPrice;
    }
}