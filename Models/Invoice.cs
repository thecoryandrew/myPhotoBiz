
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyPhotoBiz.Enums;

namespace MyPhotoBiz.Models
{
    // TODO: [CRITICAL] Amount is overwritten by ApplyPaymentAsync - create separate Payment model
    // TODO: [HIGH] Add PaymentMethod field (Cash, Card, BankTransfer, PayPal, etc.)
    // TODO: [HIGH] Add PaymentTransactionId for payment gateway reference
    // TODO: [HIGH] Add PartiallyPaid status for partial payments
    // TODO: [HIGH] Add Refunded status and RefundAmount field
    // TODO: [MEDIUM] Add soft delete (IsDeleted flag)
    // TODO: [MEDIUM] Add Currency field for international clients
    // TODO: [MEDIUM] Add ReminderSentDate to track when reminders were sent
    // TODO: [FEATURE] Add recurring invoice support
    // TODO: [FEATURE] Add deposit/retainer tracking
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

        // Client Navigation properties (via ClientProfile)
        public int? ClientProfileId { get; set; }
        public virtual ClientProfile? ClientProfile { get; set; }

        // PhotoShoot Navigation properties
        public int? PhotoShootId { get; set; }
        public PhotoShoot? PhotoShoot { get; set; }

        public ICollection<InvoiceItem>? InvoiceItems { get; set; }

        // Computed property
        [NotMapped]
        public decimal TotalAmount => Amount + Tax;


    }
}