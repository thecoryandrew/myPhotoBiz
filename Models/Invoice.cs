
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyPhotoBiz.Enums;
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
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tax cannot be negative")]
        public decimal Tax { get; set; }

        public string? Notes { get; set; }

        public DateTime? PaidDate { get; set; }

        // Client Navigation properties
        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        // PhotoShoot Navigation properties
        public int? PhotoShootId { get; set; }
        public PhotoShoot? PhotoShoot { get; set; }

        public ICollection<InvoiceItem>? InvoiceItems { get; set; }

        // Computed property
        [NotMapped]
        public decimal TotalAmount => Amount + Tax;


    }
}