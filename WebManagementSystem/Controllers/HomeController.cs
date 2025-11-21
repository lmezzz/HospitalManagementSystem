
using Microsoft.AspNetCore.Mvc;
namespace WebManagementSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
