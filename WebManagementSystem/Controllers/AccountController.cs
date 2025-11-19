using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;
using static WebManagementSystem.Services.PasswordHelper;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebManagementSystem.Controllers;

public class AccountController : Controller
{

    private readonly HMS_DB _context;

    public AccountController(HMS_DB context)
    {
        _context = context;
    }
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username == model.Username);

        if(user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            Console.WriteLine("User not found");
            return View(model);
        }

        var hashedInput = HashPassword(model.Password);

        if(user.PasswordHash != hashedInput)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }
        // Authentication successful

        //Now am gonna be creating Auth cookies 
        //Claims is just like a key value pair to store user info in the cookie but its more secure because whenever 
        //there is a change in the cookie the claims will be invalidated
        var claims = new List<Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username ?? ""),
            new(System.Security.Claims.ClaimTypes.Role, user.Role?.RoleName ?? "User")
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "LoginAuthCookie");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("LoginAuthCookie", principal);

        return RedirectToAction("Index", "Home");
    }
}
