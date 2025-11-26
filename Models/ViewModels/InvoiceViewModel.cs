using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyPhotoBiz.Models; // Add this to access InvoiceStatus enum

namespace MyPhotoBiz.Models.ViewModels
{

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
}