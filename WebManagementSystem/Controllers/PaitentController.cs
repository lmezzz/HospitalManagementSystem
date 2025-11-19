using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult LabReports()
        {
            return View();
        }

        public IActionResult Prescriptions()
        {
            return View();
        }

        public IActionResult Billing()
        {
            return View();
        }

        public IActionResult Pharmacy()
        {
            return View();
        }
    }

}
