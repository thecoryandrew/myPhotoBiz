
// Controllers/HomeController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyPhotoBiz.Services;
using System.Diagnostics;

namespace MyPhotoBiz.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                var dashboardData = await _dashboardService.GetDashboardDataAsync();
                return View("Dashboard", dashboardData);
            }

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = await _dashboardService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
