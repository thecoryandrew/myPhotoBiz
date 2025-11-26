using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IPdfService 
    {
        Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);
    }
}
