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

    private readonly HmsContext _context;

    public AccountController(HmsContext context)
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

        // var hashedInput = HashPassword(model.Password);
        var hashedInput = model.Password; // For demonstration purposes only. Do not use plain text passwords in production.

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

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("LoginAuthCookie");
        return RedirectToAction("Login", "Account");
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterPatientViewModel model)
    {

        
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            Username = "pt-" + model.FirstName.ToLower(),
            // PasswordHash = HashPassword(model.Password),
            PasswordHash = model.Password, // For demonstration purposes only. Do not use plain text passwords in production.
            RoleId = 2, // Assuming 2 is the RoleId for Patients
            //Email = model.email,
            FullName = model.FirstName+model.LastName,
            IsActive = true,
        };
            var now = DateTime.UtcNow;
            var createdAt = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
            user.CreatedAt = createdAt;

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        var patient = new Patient
        {
            FullName = model.FirstName+model.LastName,
            Gender = model.Gender,
            DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth),
            Phone = model.PhoneNumber,
            Cnic = model.Cnic,
            Address = model.Address,
            EmergencyContact = model.EmergencyContact,
            Allergies = model.Allergies,
            ChronicConditions = model.ChronicDiseases,
            UserId = user.UserId // Link to the created AppUser
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();


        return RedirectToAction("Login", "Account");
    }

}
