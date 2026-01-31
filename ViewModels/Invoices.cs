using Microsoft.AspNetCore.Identity;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.ViewModels
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
        [Display(Name = "Status")]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

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

        [Required(ErrorMessage = "Item description is required")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal Total => Quantity * UnitPrice;
    }

     public class InvoiceDashboardViewModel
    {

        public decimal TotalRevenue { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int PendingInvoices { get; set; }
        public int OverdueInvoices { get; set; }
        public IEnumerable<Invoice> RecentInvoices { get; set; } = new List<Invoice>();
        public IEnumerable<Invoice> OverdueInvoicesList { get; set; } = new List<Invoice>();
    }

    public class InvoiceViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }


        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "Tax")]
        [DataType(DataType.Currency)]
        public decimal Tax { get; set; }

        public string? Notes { get; set; }

        [Display(Name = "Paid Date")]
        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        public InvoiceStatus Status { get; set; }
        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        public string? ClientName { get; set; }

        [EmailAddress]
        public string? ClientEmail { get; set; }


        [Display(Name = "Photo Shoot")]
        public string? PhotoShootTitle { get; set; }

        public List<InvoiceItemViewModel> InvoiceItems { get; set; } = new List<InvoiceItemViewModel>();

        [NotMapped]
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => Amount + Tax;
    }
public class InvoiceListViewModel
{
    public IEnumerable<Invoice> Invoices { get; set; } = new List<Invoice>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public int? ClientId { get; set; }
    public InvoiceStatus? Status { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

}