// ViewModels/InvoiceListViewModel.cs
using MyPhotoBiz.Models;
namespace MyPhotoBiz.Models.ViewModels;
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
