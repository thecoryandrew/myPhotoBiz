// Controllers/PrintOrderController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPhotoBiz.Controllers
{
    [AllowAnonymous] // Print orders accessible to clients via session token
    public class PrintOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrintOrderController> _logger;

        public PrintOrderController(ApplicationDbContext context, ILogger<PrintOrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display cart preview with favorite photos for ordering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CartPreview(string sessionToken)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionToken))
                    return RedirectToAction("Index", "Gallery");

                var session = await _context.GallerySessions
                    .Include(s => s.Gallery)
                    .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

                if (session == null)
                    return RedirectToAction("Index", "Gallery");

                // Check if gallery is still active
                if (!session.Gallery.IsActive || session.Gallery.ExpiryDate < DateTime.UtcNow)
                    return RedirectToAction("Index", "Gallery");

                var favorites = await _context.Proofs
                    .Where(p => p.GallerySessionId == session.Id && p.IsFavorite && p.Photo != null)
                    .Include(p => p.Photo)
                    .Select(p => p.Photo!)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                var printPrices = await _context.PrintPricings
                    .OrderBy(p => p.Size)
                    .ThenBy(p => p.FinishType)
                    .ToListAsync();

                ViewBag.SessionToken = sessionToken;
                ViewBag.GalleryName = session.Gallery.Name;
                ViewBag.BrandColor = session.Gallery.BrandColor ?? "#2c3e50";
                ViewBag.PrintPrices = printPrices;

                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart preview");
                TempData["Error"] = "An error occurred while loading your cart. Please try again.";
                return RedirectToAction("Index", "Gallery");
            }
        }

        /// <summary>
        /// Place a print order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PrintOrderViewModel model, string sessionToken)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionToken))
                    return Unauthorized();

                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", "Please fill in all required fields correctly.");
                    return RedirectToAction("CartPreview", new { sessionToken });
                }

                var session = await _context.GallerySessions
                    .Include(s => s.Gallery)
                    .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

                if (session == null)
                    return Unauthorized();

                // Validate gallery is still active
                if (!session.Gallery.IsActive || session.Gallery.ExpiryDate < DateTime.UtcNow)
                    return Unauthorized();

                // Validate items exist
                if (model.Items == null || model.Items.Count == 0)
                {
                    ModelState.AddModelError("", "Please add items to your order.");
                    return RedirectToAction("CartPreview", new { sessionToken });
                }

                var order = new PrintOrder
                {
                    GallerySessionId = session.Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                    ClientName = model.ClientName?.Trim(),
                    ClientEmail = model.ClientEmail?.Trim(),
                    ClientPhone = model.ClientPhone?.Trim(),
                    Status = OrderStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    TotalPrice = 0,
                    PrintLabOrderId = null,
                };

                decimal totalPrice = 0;
                int itemCount = 0;

                foreach (var item in model.Items)
                {
                    // Validate item
                    if (item.Quantity <= 0 || string.IsNullOrEmpty(item.Size) || string.IsNullOrEmpty(item.FinishType))
                        continue;

                    // Verify photo belongs to an album in the gallery
                    var photo = await _context.Photos
                        .Include(p => p.Album)
                            .ThenInclude(a => a.Galleries)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == item.PhotoId && p.Album.Galleries.Any(g => g.Id == session.GalleryId));

                    if (photo == null)
                        continue;

                    var pricing = await _context.PrintPricings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Size == item.Size && p.FinishType == item.FinishType);

                    if (pricing == null)
                        continue;

                    var printItem = new PrintItem
                    {
                        PhotoId = item.PhotoId,
                        Size = item.Size,
                        FinishType = item.FinishType,
                        Quantity = item.Quantity,
                        UnitPrice = pricing.Price,
                        PrintOrder = order
                    };

                    order.Items.Add(printItem);
                    totalPrice += pricing.Price * item.Quantity;
                    itemCount++;
                }

                // Validate we have at least one item
                if (itemCount == 0)
                {
                    ModelState.AddModelError("", "No valid items were added to the order.");
                    return RedirectToAction("CartPreview", new { sessionToken });
                }

                order.TotalPrice = totalPrice;
                _context.PrintOrders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} created successfully for {ClientEmail}", order.OrderNumber, order.ClientEmail);

                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while placing order");
                ModelState.AddModelError("", "An error occurred while saving your order. Please try again.");
                return RedirectToAction("CartPreview", new { sessionToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["Error"] = "An unexpected error occurred. Please try again.";
                return RedirectToAction("CartPreview", new { sessionToken });
            }
        }

        /// <summary>
        /// Display order confirmation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            try
            {
                var order = await _context.PrintOrders
                    .Include(o => o.Session)
                    .ThenInclude(s => s!.Gallery)
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Photo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return NotFound();

                ViewBag.BrandColor = order.Session?.Gallery?.BrandColor ?? "#2c3e50";
                ViewBag.GalleryName = order.Session?.Gallery?.Name ?? "Photo Gallery";

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get order details via API for AJAX
        /// </summary>
        [HttpGet]
        [Route("api/printorder/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                var order = await _context.PrintOrders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Photo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return NotFound();

                return Ok(new
                {
                    order.Id,
                    order.OrderNumber,
                    order.ClientName,
                    order.ClientEmail,
                    order.ClientPhone,
                    order.TotalPrice,
                    order.Status,
                    order.CreatedDate,
                    Items = order.Items.Select(i => new
                    {
                        i.Id,
                        PhotoTitle = i.Photo?.Title,
                        i.Size,
                        i.FinishType,
                        i.Quantity,
                        i.UnitPrice,
                        Total = i.UnitPrice * i.Quantity
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details");
                return StatusCode(500, new { message = "An error occurred while retrieving order details." });
            }
        }
    }
}
