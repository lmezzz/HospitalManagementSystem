using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize(Roles = "Doctor")]
public class DoctorController : Controller
{
    private readonly HmsContext _context;

    public DoctorController(HmsContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var viewModel = new DoctorDashboardViewModel
        {
            DoctorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
            TodaysAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == userId &&
                               a.ScheduledTime.HasValue &&
                               a.ScheduledTime.Value.Date == DateTime.Today),
            CompletedToday = await _context.Appointments
                .CountAsync(a => a.DoctorId == userId &&
                               a.ScheduledTime.HasValue &&
                               a.ScheduledTime.Value.Date == DateTime.Today &&
                               a.Status == "Completed"),
            PendingToday = await _context.Appointments
                .CountAsync(a => a.DoctorId == userId &&
                               a.ScheduledTime.HasValue &&
                               a.ScheduledTime.Value.Date == DateTime.Today &&
                               a.Status == "Scheduled"),
            TotalPatientsThisMonth = await _context.Visits
                .Where(v => v.DoctorId == userId &&
                           v.VisitTime.HasValue &&
                           v.VisitTime.Value.Month == DateTime.Now.Month &&
                           v.VisitTime.Value.Year == DateTime.Now.Year)
                .Select(v => v.PatientId)
                .Distinct()
                .CountAsync()
        };

        // Today's schedule
        viewModel.TodaysSchedule = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == userId &&
                       a.ScheduledTime.HasValue &&
                       a.ScheduledTime.Value.Date == DateTime.Today)
            .OrderBy(a => a.ScheduledTime)
            .Select(a => new TodaysAppointmentDto
            {
                AppointmentId = a.AppointmentId,
                ScheduledTime = a.ScheduledTime!.Value,
                PatientName = a.Patient!.FullName ?? "",
                Reason = a.Reason ?? "",
                Status = a.Status ?? ""
            })
            .ToListAsync();

        // Recent visits
        viewModel.RecentVisits = await _context.Visits
            .Include(v => v.Patient)
            .Where(v => v.DoctorId == userId)
            .OrderByDescending(v => v.VisitTime)
            .Take(10)
            .Select(v => new RecentVisitDto
            {
                VisitId = v.VisitId,
                VisitTime = v.VisitTime!.Value,
                PatientName = v.Patient!.FullName ?? "",
                Diagnosis = v.Diagnosis ?? ""
            })
            .ToListAsync();

        return View(viewModel);
    }

    public async Task<IActionResult> Appointments(DateTime? date)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var selectedDate = date ?? DateTime.Today;

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == userId &&
                       a.ScheduledTime.HasValue &&
                       a.ScheduledTime.Value.Date == selectedDate.Date)
            .OrderBy(a => a.ScheduledTime)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                PatientName = a.Patient!.FullName ?? "",
                ScheduledTime = a.ScheduledTime,
                Reason = a.Reason,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var viewModel = new AppointmentListViewModel
        {
            Appointments = appointments,
            TotalCount = appointments.Count
        };

        ViewBag.SelectedDate = selectedDate;
        return View(viewModel);
    }

    public async Task<IActionResult> Patients(string search = "", int page = 1, int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Get distinct patients who have visited this doctor
        var query = _context.Visits
            .Include(v => v.Patient)
            .Where(v => v.DoctorId == userId)
            .Select(v => v.Patient!)
            .Distinct()
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.FullName!.Contains(search) ||
                p.Phone!.Contains(search) ||
                p.Cnic!.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var patients = await query
            .OrderBy(p => p.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientDto
            {
                PatientId = p.PatientId,
                FullName = p.FullName ?? "",
                Gender = p.Gender ?? "",
                DateOfBirth = p.DateOfBirth,
                Age = p.DateOfBirth.HasValue
                    ? DateTime.Today.Year - p.DateOfBirth.Value.Year
                    : null,
                Phone = p.Phone ?? "",
                CNIC = p.Cnic ?? "",
                Email = p.AppUser!.Email ?? "",
                LastVisit = _context.Visits
                    .Where(v => v.PatientId == p.PatientId && v.DoctorId == userId)
                    .OrderByDescending(v => v.VisitTime)
                    .Select(v => v.VisitTime)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var viewModel = new PatientListViewModel
        {
            Patients = patients,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Reports()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endDate = DateTime.Today;

        var viewModel = new DoctorDashboardViewModel
        {
            DoctorName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
            TodaysAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == userId &&
                               a.ScheduledTime >= startDate &&
                               a.ScheduledTime <= endDate),
            TotalPatientsThisMonth = await _context.Visits
                .Where(v => v.DoctorId == userId &&
                           v.VisitTime >= startDate &&
                           v.VisitTime <= endDate)
                .Select(v => v.PatientId)
                .Distinct()
                .CountAsync()
        };

        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;

        return View(viewModel);
    }

    // Additional actions for doctor workflow
    [HttpGet]
    public async Task<IActionResult> CurrentSession(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Visits)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
            return NotFound();

        var visit = appointment.Visits.FirstOrDefault();

        ViewBag.Appointment = appointment;
        ViewBag.Visit = visit;
        ViewBag.Patient = appointment.Patient;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> LabRequest(int visitId)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .FirstOrDefaultAsync(v => v.VisitId == visitId);

        if (visit == null)
            return NotFound();

        var tests = await _context.LabTests
            .Select(t => new LabTestSelectDto
            {
                LabTestId = t.LabTestId,
                TestName = t.TestName ?? "",
                Category = t.Category ?? "",
                Cost = t.Cost ?? 0,
                Description = t.Description ?? ""
            })
            .ToListAsync();

        ViewBag.Visit = visit;
        ViewBag.AvailableTests = tests;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Prescriptions(int page = 1, int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var query = _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .Where(p => p.DoctorId == userId);

        var totalCount = await query.CountAsync();

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                VisitId = p.VisitId,
                PatientName = p.Visit!.Patient!.FullName ?? "",
                CreatedAt = p.CreatedAt,
                ItemCount = p.PrescriptionItems.Count
            })
            .ToListAsync();

        var viewModel = new PrescriptionListViewModel
        {
            Prescriptions = prescriptions,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> LabReports(int page = 1, int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var query = _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.LabTest)
            .Where(l => l.DoctorId == userId);

        var totalCount = await query.CountAsync();

        var labOrders = await query
            .OrderByDescending(l => l.OrderTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LabOrderDto
            {
                LabOrderId = l.LabOrderId,
                PatientName = l.Patient!.FullName ?? "",
                TestName = l.LabTest!.TestName ?? "",
                OrderTime = l.OrderTime!.Value,
                Status = l.Status ?? "",
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        var viewModel = new LabOrderListViewModel
        {
            Orders = labOrders,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(DateTime? date)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var selectedDate = date ?? DateTime.Today;

        var schedules = await _context.Schedules
            .Where(s => s.DoctorId == userId &&
                       s.SlotDate.HasValue &&
                       s.SlotDate.Value.Date == selectedDate.Date)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        ViewBag.SelectedDate = selectedDate;
        ViewBag.Schedules = schedules;

        return View();
    }

    // AJAX endpoints
    [HttpGet]
    public async Task<IActionResult> GetTodaysAppointments()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == userId &&
                       a.ScheduledTime.HasValue &&
                       a.ScheduledTime.Value.Date == DateTime.Today)
            .OrderBy(a => a.ScheduledTime)
            .Select(a => new
            {
                a.AppointmentId,
                PatientName = a.Patient!.FullName,
                ScheduledTime = a.ScheduledTime!.Value.ToString("HH:mm"),
                a.Reason,
                a.Status
            })
            .ToListAsync();

        return Json(appointments);
    }
}
