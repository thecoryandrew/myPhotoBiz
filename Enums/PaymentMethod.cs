using System.ComponentModel.DataAnnotations;

namespace MyPhotoBiz.Enums
{
    public enum PaymentMethod
    {
        [Display(Name = "Cash")]
        Cash = 0,
        [Display(Name = "Credit Card")]
        CreditCard = 1,
        [Display(Name = "Debit Card")]
        DebitCard = 2,
        [Display(Name = "Bank Transfer")]
        BankTransfer = 3,
        [Display(Name = "PayPal")]
        PayPal = 4,
        [Display(Name = "Venmo")]
        Venmo = 5,
        [Display(Name = "Other")]
        Other = 6
    }
}
