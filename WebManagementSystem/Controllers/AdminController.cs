using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly HmsContext _context;

    public AdminController(HmsContext context)
    {
        _context = context;
    }

    // Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var viewModel = new AdminDashboardViewModel
        {
            TotalPatients = await _context.Patients.CountAsync(),
            TotalDoctors = await _context.AppUsers.CountAsync(u => u.RoleId == 3), // Doctor role
            TotalAppointmentsToday = await _context.Appointments
                .CountAsync(a => a.ScheduledTime.HasValue && a.ScheduledTime.Value.Date == DateTime.Today),
            TotalPendingBills = await _context.Bills.CountAsync(b => b.Status == "Unpaid" || b.Status == "Partial"),
            TotalRevenueToday = await _context.Payments
                .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value.Date == DateTime.Today)
                .SumAsync(p => p.AmountPaid ?? 0),
            TotalRevenueMonth = await _context.Payments
                .Where(p => p.PaymentTime.HasValue &&
                           p.PaymentTime.Value.Month == DateTime.Now.Month &&
                           p.PaymentTime.Value.Year == DateTime.Now.Year)
                .SumAsync(p => p.AmountPaid ?? 0),
            TotalPendingLabOrders = await _context.LabOrders.CountAsync(l => l.Status == "Pending"),
            LowStockMedicationsCount = await _context.Medications
                .CountAsync(m => m.StockQuantity <= m.LowStockThreshold)
        };

        // Recent Appointments
        viewModel.RecentAppointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentAppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientName = a.Patient!.FullName ?? "",
                DoctorName = a.Doctor!.FullName ?? "",
                ScheduledTime = a.ScheduledTime ?? DateTime.MinValue,
                Status = a.Status ?? "Unknown"
            })
            .ToListAsync();

        // Top Doctors by patient count
        viewModel.TopDoctors = await _context.Visits
            .Include(v => v.Doctor)
            .GroupBy(v => v.DoctorId)
            .Select(g => new TopDoctorDto
            {
                DoctorName = g.First().Doctor!.FullName ?? "",
                PatientCount = g.Select(v => v.PatientId).Distinct().Count(),
                Revenue = 0 // TODO: Calculate from bills
            })
            .OrderByDescending(d => d.PatientCount)
            .Take(5)
            .ToListAsync();

        // Revenue chart data for last 7 days
        viewModel.RevenueChartData = await _context.Payments
            .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value >= DateTime.Today.AddDays(-7))
            .GroupBy(p => p.PaymentTime!.Value.Date)
            .Select(g => new RevenueChartDto
            {
                Date = g.Key.ToString("MMM dd"),
                Amount = g.Sum(p => p.AmountPaid ?? 0)
            })
            .ToListAsync();

        return View(viewModel);
    }

    // User Management
    [HttpGet]
    public async Task<IActionResult> Users(int page = 1, int pageSize = 20, int? roleFilter = null, bool? activeFilter = null)
    {
        var query = _context.AppUsers.Include(u => u.Role).AsQueryable();

        if (roleFilter.HasValue)
            query = query.Where(u => u.RoleId == roleFilter.Value);

        if (activeFilter.HasValue)
            query = query.Where(u => u.IsActive == activeFilter.Value);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username ?? "",
                Email = u.Email ?? "",
                FullName = u.FullName ?? "",
                RoleId = u.RoleId ?? 0,
                RoleName = u.Role!.RoleName ?? "",
                IsActive = u.IsActive ?? true,
                CreatedAt = u.CreatedAt ?? DateTime.MinValue
            })
            .ToListAsync();

        var viewModel = new UserListViewModel
        {
            Users = users,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            RoleFilter = roleFilter,
            ActiveFilter = activeFilter
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var viewModel = new CreateUserViewModel
        {
            AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName ?? ""
                })
                .ToListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                .ToListAsync();
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            model.AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                .ToListAsync();
            return View(model);
        }

        // Check if username already exists
        if (await _context.AppUsers.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists");
            model.AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                .ToListAsync();
            return View(model);
        }

        var user = new AppUser
        {
            Username = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            PasswordHash = model.Password, // Note: Password hashing disabled as per user request
            RoleId = model.RoleId,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "User created successfully", userId = user.UserId });
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var viewModel = new EditUserViewModel
        {
            UserId = user.UserId,
            Username = user.Username ?? "",
            Email = user.Email ?? "",
            FullName = user.FullName ?? "",
            RoleId = user.RoleId ?? 0,
            IsActive = user.IsActive ?? true,
            CurrentRole = user.Role?.RoleName ?? "",
            CreatedAt = user.CreatedAt ?? DateTime.MinValue,
            AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                .ToListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _context.Roles
                .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                .ToListAsync();
            return View(model);
        }

        var user = await _context.AppUsers.FindAsync(model.UserId);
        if (user == null)
            return NotFound();

        // Check if password change is requested
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                model.AvailableRoles = await _context.Roles
                    .Select(r => new RoleSelectDto { RoleId = r.RoleId, RoleName = r.RoleName ?? "" })
                    .ToListAsync();
                return View(model);
            }
            user.PasswordHash = model.NewPassword; // Note: Password hashing disabled
        }

        user.Email = model.Email;
        user.FullName = model.FullName;
        user.RoleId = model.RoleId;
        user.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "User updated successfully" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "User not found" });

        // Don't allow deleting yourself
        var currentUsername = User.Identity?.Name;
        if (user.Username == currentUsername)
            return Json(new { success = false, message = "Cannot delete your own account" });

        // Soft delete by deactivating
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "User deactivated successfully" });
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var viewModel = new UserDetailsViewModel
        {
            UserId = user.UserId,
            Username = user.Username ?? "",
            Email = user.Email ?? "",
            FullName = user.FullName ?? "",
            RoleName = user.Role?.RoleName ?? "",
            IsActive = user.IsActive ?? true,
            CreatedAt = user.CreatedAt ?? DateTime.MinValue
        };

        // Add role-specific stats
        if (user.Role?.RoleName == "Doctor")
        {
            viewModel.DoctorStats = new DoctorStatsDto
            {
                TotalAppointments = await _context.Appointments.CountAsync(a => a.DoctorId == id),
                TotalVisits = await _context.Visits.CountAsync(v => v.DoctorId == id),
                TotalPatients = await _context.Visits
                    .Where(v => v.DoctorId == id)
                    .Select(v => v.PatientId)
                    .Distinct()
                    .CountAsync(),
                LastAppointment = await _context.Appointments
                    .Where(a => a.DoctorId == id)
                    .OrderByDescending(a => a.ScheduledTime)
                    .Select(a => a.ScheduledTime)
                    .FirstOrDefaultAsync()
            };
        }
        else if (user.Role?.RoleName == "Patient")
        {
            var patientId = await _context.Patients
                .Where(p => p.UserId == id)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();

            if (patientId > 0)
            {
                var totalBilled = await _context.Bills
                    .Where(b => b.PatientId == patientId)
                    .SumAsync(b => b.TotalAmount ?? 0);

                var totalPaid = await _context.Payments
                    .Where(p => _context.Bills.Any(b => b.BillId == p.BillId && b.PatientId == patientId))
                    .SumAsync(p => p.AmountPaid ?? 0);

                viewModel.PatientStats = new PatientStatsDto
                {
                    TotalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patientId),
                    TotalVisits = await _context.Visits.CountAsync(v => v.PatientId == patientId),
                    TotalBillAmount = totalBilled,
                    OutstandingBalance = totalBilled - totalPaid,
                    LastVisit = await _context.Visits
                        .Where(v => v.PatientId == patientId)
                        .OrderByDescending(v => v.VisitTime)
                        .Select(v => v.VisitTime)
                        .FirstOrDefaultAsync()
                };
            }
        }

        return View(viewModel);
    }

    // Reports
    [HttpGet]
    public async Task<IActionResult> SystemReport()
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var viewModel = new SystemReportViewModel
        {
            ReportDate = DateTime.Now,
            TotalUsers = await _context.AppUsers.CountAsync(),
            ActiveUsers = await _context.AppUsers.CountAsync(u => u.IsActive == true),
            TotalDoctors = await _context.AppUsers.CountAsync(u => u.RoleId == 3),
            TotalPatients = await _context.Patients.CountAsync(),

            TotalAppointments = await _context.Appointments.CountAsync(),
            AppointmentsToday = await _context.Appointments
                .CountAsync(a => a.ScheduledTime.HasValue && a.ScheduledTime.Value.Date == today),
            AppointmentsThisWeek = await _context.Appointments
                .CountAsync(a => a.ScheduledTime.HasValue && a.ScheduledTime.Value >= weekStart),
            AppointmentsThisMonth = await _context.Appointments
                .CountAsync(a => a.ScheduledTime.HasValue && a.ScheduledTime.Value >= monthStart),

            RevenueToday = await _context.Payments
                .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value.Date == today)
                .SumAsync(p => p.AmountPaid ?? 0),
            RevenueThisWeek = await _context.Payments
                .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value >= weekStart)
                .SumAsync(p => p.AmountPaid ?? 0),
            RevenueThisMonth = await _context.Payments
                .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value >= monthStart)
                .SumAsync(p => p.AmountPaid ?? 0),

            TotalVisits = await _context.Visits.CountAsync(),
            VisitsToday = await _context.Visits
                .CountAsync(v => v.VisitTime.HasValue && v.VisitTime.Value.Date == today),
            PendingLabOrders = await _context.LabOrders.CountAsync(l => l.Status == "Pending"),

            TotalMedications = await _context.Medications.CountAsync(),
            LowStockMedications = await _context.Medications
                .CountAsync(m => m.StockQuantity <= m.LowStockThreshold)
        };

        // Appointment trends (last 30 days)
        viewModel.AppointmentTrends = await _context.Appointments
            .Where(a => a.ScheduledTime.HasValue && a.ScheduledTime.Value >= today.AddDays(-30))
            .GroupBy(a => a.ScheduledTime!.Value.Date)
            .Select(g => new AppointmentTrendDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        // Revenue trends (last 30 days)
        viewModel.RevenueTrends = await _context.Payments
            .Where(p => p.PaymentTime.HasValue && p.PaymentTime.Value >= today.AddDays(-30))
            .GroupBy(p => p.PaymentTime!.Value.Date)
            .Select(g => new RevenueTrendDto
            {
                Date = g.Key,
                Amount = g.Sum(p => p.AmountPaid ?? 0)
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        return View(viewModel);
    }

    // AJAX endpoints
    [HttpGet]
    public async Task<IActionResult> GetUsersList(int page = 1, int pageSize = 10, string search = "")
    {
        var query = _context.AppUsers.Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Username!.Contains(search) ||
                u.FullName!.Contains(search) ||
                u.Email!.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                u.Email,
                u.FullName,
                RoleName = u.Role!.RoleName,
                u.IsActive,
                CreatedAt = u.CreatedAt!.Value.ToString("MMM dd, yyyy")
            })
            .ToListAsync();

        return Json(new
        {
            data = users,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user == null)
            return Json(new { success = false, message = "User not found" });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = user.IsActive, message = $"User {(user.IsActive == true ? "activated" : "deactivated")} successfully" });
    }
}
