using Microsoft.AspNetCore.Mvc;

namespace HMS.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View("login");  
        }
    }
}
