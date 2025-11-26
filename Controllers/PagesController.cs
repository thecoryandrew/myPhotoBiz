using Microsoft.AspNetCore.Mvc;

namespace myPhotoBiz.Controllers
{
    public class PagesController : Controller
    {
        public IActionResult Index() => View();
    }
}