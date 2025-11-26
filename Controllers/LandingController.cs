using Microsoft.AspNetCore.Mvc;

namespace myPhotoBiz.Controllers
{
    public class LandingController : Controller
    {
        public IActionResult Index() => View();
    }
}