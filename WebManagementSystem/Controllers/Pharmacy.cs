using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class PharmacyController : Controller
    {
      
        public IActionResult Index()
        {
            return View();  
        }

        public IActionResult IssueMedicine()
        {
            return View();   
        }

       
        public IActionResult Inventory()
        {
            return View();   
        }
    }
}
