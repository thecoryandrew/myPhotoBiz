
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPhotoBiz.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        public DateTime DueDate { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        public string? Notes { get; set; }
        public DateTime? PaidDate { get; set; }

        // Navigation properties
        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public int? PhotoShootId { get; set; }
        public PhotoShoot? PhotoShoot { get; set; }

        public ICollection<InvoiceItem>? InvoiceItems { get; set; }

        // Computed property
        [NotMapped]
        public decimal TotalAmount => Amount + Tax;


    }

    public class InvoiceItem
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal Total => Quantity * UnitPrice;

        // Foreign key
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }

    public enum InvoiceStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,
        [Display(Name = "Pending")]
        Pending = 1,
        [Display(Name = "Paid")]
        Paid = 2,
        [Display(Name = "Overdue")]
        Overdue = 3,
        [Display(Name = "Cancelled")]
        Cancelled = 4
    }
}