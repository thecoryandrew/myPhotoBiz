using MyPhotoBiz.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text;

namespace MyPhotoBiz.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice)
    {
        try
        {
            var html = await GenerateInvoiceHtmlAsync(invoice);
            return await GenerateInvoicePdfFromHtmlAsync(html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", invoice.Id);
            throw;
        }
    }

    private static string? FindChromePath()
    {
        // Common Chrome paths on Linux
        var possiblePaths = new[]
        {
            "/opt/google/chrome/chrome",
            "/usr/bin/google-chrome",
            "/usr/bin/google-chrome-stable",
            "/usr/bin/chrome",
            "/usr/bin/chromium",
            "/usr/bin/chromium-browser",
            "/snap/bin/chromium"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    private async Task<byte[]> GenerateInvoicePdfFromHtmlAsync(string html)
    {
        try
        {
            var browserWSEndpoint = Environment.GetEnvironmentVariable("CHROME_WS_ENDPOINT");
            IBrowser? browser = null;

            if (!string.IsNullOrEmpty(browserWSEndpoint))
            {
                // Try to connect to existing Chrome instance
                try
                {
                    browser = await Puppeteer.ConnectAsync(new ConnectOptions { BrowserWSEndpoint = browserWSEndpoint });
                    _logger.LogInformation("Connected to existing Chrome instance at {endpoint}", browserWSEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to connect to Chrome at {endpoint}", browserWSEndpoint);
                }
            }

            if (browser == null)
            {
                // Try to find local Chrome installation
                var executablePath = FindChromePath();
                if (!string.IsNullOrEmpty(executablePath))
                {
                    try
                    {
                        browser = await Puppeteer.LaunchAsync(new LaunchOptions
                        {
                            Headless = true,
                            ExecutablePath = executablePath,
                            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                        });
                        _logger.LogInformation("Launched local Chrome from {path}", executablePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to launch Chrome from {path}", executablePath);
                    }
                }
            }

            // If all else fails, try to download Chrome
            if (browser == null)
            {
                _logger.LogInformation("Attempting to download Chrome...");
                await new BrowserFetcher().DownloadAsync();
                browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });
            }

            await using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                DisplayHeaderFooter = false,
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Bottom = "20px",
                    Left = "20px",
                    Right = "20px"
                }
            });

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from HTML");
            throw;
        }
    }

    public async Task<string> GenerateInvoiceHtmlAsync(Invoice invoice)
    {
        try
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Invoice</title>");
            html.AppendLine(GetInvoiceStyles());
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='invoice'>");

            // Header with invoice number
            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>INVOICE</h1>");
            html.AppendLine("<p><strong>Invoice #:</strong> " + (invoice.InvoiceNumber ?? "N/A") + "</p>");
            html.AppendLine("</div>");

            // Invoice details
            html.AppendLine("<div class='invoice-details'>");
            html.AppendLine("<div class='row'>");
            html.AppendLine("<div class='col'>");
            html.AppendLine("<h3>Bill To</h3>");
            if (invoice.Client != null)
            {
                html.AppendLine("<p><strong>" + invoice.Client.FirstName + " " + invoice.Client.LastName + "</strong></p>");
                if (!string.IsNullOrWhiteSpace(invoice.Client.Email))
                    html.AppendLine("<p>" + invoice.Client.Email + "</p>");
                if (!string.IsNullOrWhiteSpace(invoice.Client.PhoneNumber))
                    html.AppendLine("<p>" + invoice.Client.PhoneNumber + "</p>");
                if (!string.IsNullOrWhiteSpace(invoice.Client.Address))
                    html.AppendLine("<p>" + invoice.Client.Address + "</p>");
            }
            html.AppendLine("</div>");
            html.AppendLine("<div class='col text-right'>");
            html.AppendLine("<p><strong>Invoice Date:</strong> " + invoice.InvoiceDate.ToString("MMMM dd, yyyy") + "</p>");
            html.AppendLine("<p><strong>Due Date:</strong> " + invoice.DueDate.ToString("MMMM dd, yyyy") + "</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Items table
            html.AppendLine("<table class='items'>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>Description</th>");
            html.AppendLine("<th>Quantity</th>");
            html.AppendLine("<th>Unit Price</th>");
            html.AppendLine("<th>Total</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            if (invoice.InvoiceItems != null && invoice.InvoiceItems.Any())
            {
                foreach (var item in invoice.InvoiceItems)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine("<td>" + (item.Description ?? "") + "</td>");
                    html.AppendLine("<td style='text-align: center;'>" + item.Quantity + "</td>");
                    html.AppendLine("<td style='text-align: right;'>$" + item.UnitPrice.ToString("0.00") + "</td>");
                    html.AppendLine("<td style='text-align: right;'>$" + (item.Quantity * item.UnitPrice).ToString("0.00") + "</td>");
                    html.AppendLine("</tr>");
                }
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");

            // Totals
            html.AppendLine("<div class='totals'>");
            html.AppendLine("<div class='total-row'>");
            html.AppendLine("<span><strong>Subtotal:</strong></span>");
            html.AppendLine("<span>$" + invoice.Amount.ToString("0.00") + "</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='total-row'>");
            html.AppendLine("<span><strong>Tax:</strong></span>");
            html.AppendLine("<span>$" + invoice.Tax.ToString("0.00") + "</span>");
            html.AppendLine("</div>");
            html.AppendLine("<div class='total-row grand-total'>");
            html.AppendLine("<span><strong>TOTAL DUE:</strong></span>");
            html.AppendLine("<span>$" + (invoice.Amount + invoice.Tax).ToString("0.00") + "</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            // Notes
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                html.AppendLine("<div class='notes'>");
                html.AppendLine("<h3>Notes</h3>");
                html.AppendLine("<p>" + invoice.Notes + "</p>");
                html.AppendLine("</div>");
            }

            // Status
            html.AppendLine("<div class='status'>");
            html.AppendLine("<p><strong>Status:</strong> " + invoice.Status + "</p>");
            html.AppendLine("</div>");

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return await Task.FromResult(html.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HTML for invoice {InvoiceId}", invoice.Id);
            throw;
        }
    }

    private static string GetInvoiceStyles()
    {
        return @"<style>
            body { 
                font-family: Arial, sans-serif; 
                margin: 0;
                padding: 0;
                background: white;
            }
            .invoice { 
                max-width: 800px; 
                margin: 0 auto; 
                padding: 40px; 
                color: #333;
            }
            .header {
                margin-bottom: 30px;
                border-bottom: 3px solid #007bff;
                padding-bottom: 20px;
            }
            .header h1 {
                margin: 0;
                font-size: 32px;
                color: #007bff;
            }
            .invoice-details {
                margin: 30px 0;
                display: flex;
                justify-content: space-between;
            }
            .row {
                width: 100%;
                display: flex;
            }
            .col {
                flex: 1;
            }
            .col.text-right {
                text-align: right;
            }
            .col h3 {
                margin-top: 0;
                margin-bottom: 10px;
                color: #007bff;
            }
            .col p {
                margin: 5px 0;
                line-height: 1.5;
            }
            table.items { 
                width: 100%; 
                border-collapse: collapse; 
                margin: 30px 0; 
            }
            table.items th { 
                background-color: #f0f0f0;
                padding: 12px;
                text-align: left;
                border-bottom: 2px solid #007bff;
                font-weight: bold;
            }
            table.items td { 
                padding: 12px;
                border-bottom: 1px solid #ddd; 
            }
            table.items tr:hover {
                background-color: #f9f9f9;
            }
            .totals {
                margin: 30px 0;
                text-align: right;
                width: 100%;
                max-width: 400px;
                margin-left: auto;
                margin-right: 0;
            }
            .total-row {
                display: flex;
                justify-content: space-between;
                margin: 12px 0;
                font-size: 14px;
                padding: 8px 0;
            }
            .total-row span:first-child {
                text-align: left;
            }
            .total-row span:last-child {
                text-align: right;
                min-width: 100px;
            }
            .total-row.grand-total {
                font-size: 18px;
                font-weight: bold;
                padding: 15px 0;
                border-top: 2px solid #007bff;
                border-bottom: 2px solid #007bff;
                margin-top: 15px;
            }
            .total-row.grand-total span:last-child {
                min-width: 120px;
            }
            .notes {
                margin-top: 30px;
                padding: 15px;
                background-color: #f9f9f9;
                border-left: 4px solid #007bff;
            }
            .notes h3 {
                margin-top: 0;
                color: #007bff;
            }
            .status {
                margin-top: 20px;
                padding: 10px;
                background-color: #e8f4f8;
                border-radius: 4px;
            }
        </style>";
    }
}