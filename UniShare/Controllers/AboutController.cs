using Microsoft.AspNetCore.Mvc;

namespace UniShare.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
