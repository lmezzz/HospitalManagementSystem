using Microsoft.AspNetCore.Mvc;

namespace WebManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View("login");  
        }
        public IActionResult Register()
        {
            return View("Register");
        }
    }
}
