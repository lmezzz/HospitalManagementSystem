using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class PaitentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
