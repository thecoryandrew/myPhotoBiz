using Microsoft.AspNetCore.Mvc;

namespace myPhotoBiz.Controllers
{
    public class TopbarController : Controller
    {
        public IActionResult SubItems() => View();
        public IActionResult Tools() => View();
        public IActionResult Dark() => View();
        public IActionResult Gradient() => View();
        public IActionResult Gray() => View();
    }
}