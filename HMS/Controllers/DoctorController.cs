using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class DoctorController : Controller
    {
        // GET: /Doctor/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }

        // Optional extra actions you can add later:
        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult Patients()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }
    }
}
