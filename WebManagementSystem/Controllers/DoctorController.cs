using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class DoctorController : Controller
    {
        
        public IActionResult Dashboard()
        {
            return View();
        }

    
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
