using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {

        
        private readonly HmsContext _context;

        public DoctorController(HmsContext context)
        {
            _context = context;
        }
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

        public IActionResult LabRequest()
        {
            var UserId = int.Parse(HttpContext.User.FindFirst("UserId")!.Value);

            var patients = _context.Appointments
                .Where(p => p.DoctorId == UserId)
                .Select(p => new PatientNameViewModel {
                    Id = p.PatientId ?? 0,
                    FullName = p.Patient!.FullName ?? ""
                })
                .ToList();

            return View(patients);
        }  

        [HttpPost]
        public IActionResult LabRequest(int patientId)
        {
            return RedirectToAction("Create", "LabOrder", new { patientId = patientId });
        }      
    }
}
