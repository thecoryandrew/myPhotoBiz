using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IInvoiceService
    {
        Task UpdateInvoiceAsync(Invoice invoice);

        Task<IEnumerable<Invoice>> GetFilteredInvoicesAsync(string? search, int? clientId, InvoiceStatus? status, int page, int pageSize);
        Task<IEnumerable<Invoice>> GetClientInvoicesAsync(int clientId, InvoiceStatus? status, int page, int pageSize);
        Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count);
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
        Task<IEnumerable<Invoice>> GetInvoicesDueSoonAsync(int days);
        Task UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus status, DateTime? paidDate = null);
        Task BulkUpdateInvoiceStatusAsync(IEnumerable<int> invoiceIds, InvoiceStatus status);
        Task<Invoice?> DuplicateInvoiceAsync(int invoiceId);
        Task SendInvoiceReminderAsync(int invoiceId);
        Task SendOverdueNoticeAsync(int invoiceId);
        Task SendPaymentConfirmationAsync(int invoiceId);
        Task ApplyPaymentAsync(int invoiceId, decimal amount, DateTime paidDate);
        Task<IEnumerable<Invoice>> GetInvoicesByPhotoShootIdAsync(int photoShootId);
        Task MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate);  
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
    }
}
