using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    // TODO: [CRITICAL-DATA] ApplyPaymentAsync overwrites invoice.Amount - should create separate Payment records
    // TODO: [HIGH] Create Payment model to track payment history (amount, date, method, transactionId)
    // TODO: [HIGH] Add DeleteInvoiceAsync method - currently only soft-deletes to Draft status
    // TODO: [HIGH] Add partial payment support with PartiallyPaid status
    // TODO: [HIGH] Add Refund functionality with Refunded status
    // TODO: [MEDIUM] Add invoice status transition validation (state machine)
    // TODO: [MEDIUM] Add scheduled job to auto-mark overdue invoices
    // TODO: [FEATURE] Add PaymentMethod tracking (cash, card, bank transfer, etc.)
    // TODO: [FEATURE] Add recurring invoice support
    // TODO: [FEATURE] Add invoice PDF generation with branding
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Get Methods

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .Include(i => i.InvoiceItems)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<Invoice>> GetFilteredInvoicesAsync(
            string? searchTerm,
            int? clientId,
            InvoiceStatus? status,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(i => i.InvoiceNumber.Contains(searchTerm));

            if (clientId.HasValue)
                query = query.Where(i => i.ClientProfileId == clientId.Value);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            return await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetClientInvoicesAsync(
            int clientId,
            InvoiceStatus? status,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .Where(i => i.ClientProfileId == clientId);

            if (status.HasValue)
                query = query.Where(i => i.Status == status.Value);

            return await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count)
        {
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .OrderByDescending(i => i.InvoiceDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
        {
            var today = DateTime.Today;
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Where(i => i.DueDate < today && i.Status != InvoiceStatus.Paid)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesDueSoonAsync(int days)
        {
            var targetDate = DateTime.Today.AddDays(days);
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Where(i => i.DueDate <= targetDate && i.Status != InvoiceStatus.Paid)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByPhotoShootIdAsync(int photoShootId)
        {
            return await _context.Invoices
                .Include(i => i.ClientProfile)
                .Include(i => i.PhotoShoot)
                .Where(i => i.PhotoShootId == photoShootId)
                .ToListAsync();
        }

        #endregion

        #region Create / Update

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            if (string.IsNullOrEmpty(invoice.InvoiceNumber))
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();

            invoice.UpdatedDate = DateTime.Now;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus status, DateTime? paidDate = null)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            invoice.Status = status;
            invoice.UpdatedDate = DateTime.Now;

            if (status == InvoiceStatus.Paid && paidDate.HasValue)
                invoice.PaidDate = paidDate;

            await _context.SaveChangesAsync();
        }

        public async Task BulkUpdateInvoiceStatusAsync(IEnumerable<int> invoiceIds, InvoiceStatus status)
        {
            var invoices = await _context.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToListAsync();
            foreach (var invoice in invoices)
            {
                invoice.Status = status;
                invoice.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkInvoiceAsPaidAsync(int invoiceId, DateTime paidDate)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = paidDate;
            invoice.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task ApplyPaymentAsync(int invoiceId, decimal amount, DateTime paidDate)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidDate = paidDate;
            invoice.Amount = amount;
            invoice.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Duplicate / Reminders

        public async Task<Invoice?> DuplicateInvoiceAsync(int invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return null;

            var copy = new Invoice
            {
                ClientProfileId = invoice.ClientProfileId,
                PhotoShootId = invoice.PhotoShootId,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                Status = InvoiceStatus.Draft,
                Amount = invoice.Amount,
                Tax = invoice.Tax,
                Notes = invoice.Notes,
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                UpdatedDate = DateTime.Now,
                InvoiceItems = invoice.InvoiceItems?.Select(ii => new InvoiceItem
                {
                    Description = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice
                }).ToList()
            };

            await CreateInvoiceAsync(copy);
            return copy;
        }

        public async Task SendInvoiceReminderAsync(int invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            // TODO: Replace with real email logic
            await Task.CompletedTask;
        }

        public async Task SendOverdueNoticeAsync(int invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            // TODO: Replace with real email logic
            await Task.CompletedTask;
        }

        public async Task SendPaymentConfirmationAsync(int invoiceId)
        {
            var invoice = await GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) throw new InvalidOperationException("Invoice not found");

            // TODO: Replace with real email logic
            await Task.CompletedTask;
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            invoice.UpdatedDate = DateTime.Now;
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Helpers

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var count = await _context.Invoices.CountAsync();
            return $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{count + 1:D4}";
        }

        #endregion
    }
}
