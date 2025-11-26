// ViewModels/InvoiceDashboardViewModel.cs
using MyPhotoBiz.Models;

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