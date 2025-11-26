using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebManagementSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // If user is authenticated, redirect to their role-specific dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            return roleName switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Doctor" => RedirectToAction("Dashboard", "Doctor"),
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Pharmacist" => RedirectToAction("Index", "Pharmacy"),
                "LabTechnician" => RedirectToAction("Dashboard", "Lab"),
                "Receptionist" => RedirectToAction("Index", "Appointment"),
                "Billing" => RedirectToAction("Index", "Billing"),
                _ => View()
            };
        }

        return View();
    }
}
