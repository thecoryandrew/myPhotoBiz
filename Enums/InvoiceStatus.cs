using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Enums
{
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
        Cancelled = 4,
        [Display(Name = "Partially Paid")]
        PartiallyPaid = 5
    }
}