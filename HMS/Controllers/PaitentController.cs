using Microsoft.AspNetCore.Mvc;

namespace HMS.Controllers
{
    public class PaitentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
