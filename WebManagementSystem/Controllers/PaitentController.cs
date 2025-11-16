using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
