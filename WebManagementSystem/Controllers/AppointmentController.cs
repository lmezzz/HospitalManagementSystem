using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize]
public class AppointmentController : Controller
{
    private readonly HmsContext _context;

    public AppointmentController(HmsContext context)
    {
        _context = context;
    }

    // List appointments
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string status = "All")
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsQueryable();

        if (status != "All")
            query = query.Where(a => a.Status == status);

        // Filter by role
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (userRole == "Patient")
        {
            var patientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
            query = query.Where(a => a.PatientId == patientId);
        }
        else if (userRole == "Doctor")
        {
            query = query.Where(a => a.DoctorId == userId);
        }

        var totalCount = await query.CountAsync();

        var appointments = await query
            .OrderByDescending(a => a.ScheduledTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                PatientName = a.Patient!.FullName ?? "",
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor!.FullName ?? "",
                ScheduledTime = a.ScheduledTime,
                Reason = a.Reason,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var viewModel = new AppointmentListViewModel
        {
            Appointments = appointments,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    // Create appointment
    [HttpGet]
    public async Task<IActionResult> Create(int? patientId)
    {
        var viewModel = new CreateAppointmentViewModel();

        // Get available patients (for staff/admin)
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userRole != "Patient")
        {
            viewModel.AvailablePatients = await _context.Patients
                .Select(p => new PatientSelectDto
                {
                    PatientId = p.PatientId,
                    FullName = p.FullName ?? "",
                    Phone = p.Phone ?? "",
                    CNIC = p.Cnic ?? ""
                })
                .ToListAsync();
        }
        else
        {
            // For patients, auto-select their ID
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            viewModel.PatientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
        }

        // Get available doctors
        viewModel.AvailableDoctors = await _context.AppUsers
            .Where(u => u.RoleId == 3 && u.IsActive == true) // Doctor role
            .Select(u => new DoctorSelectDto
            {
                DoctorId = u.UserId,
                FullName = u.FullName ?? "",
                Email = u.Email ?? ""
            })
            .ToListAsync();

        if (patientId.HasValue)
            viewModel.PatientId = patientId.Value;

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        // Combine date and time
        var scheduledDateTime = model.ScheduledDate.Date.Add(model.ScheduledTime);

        // Check for existing appointment at same time
        var existingAppointment = await _context.Appointments
            .AnyAsync(a => a.DoctorId == model.DoctorId &&
                          a.ScheduledTime.HasValue &&
                          a.ScheduledTime.Value == scheduledDateTime &&
                          a.Status != "Cancelled");

        if (existingAppointment)
        {
            return Json(new { success = false, message = "Doctor already has an appointment at this time" });
        }

        // Find or create schedule slot
        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.DoctorId == model.DoctorId &&
                                     s.SlotDate.HasValue &&
                                     s.SlotDate.Value.Date == model.ScheduledDate.Date &&
                                     s.StartTime == model.ScheduledTime);

        if (schedule == null)
        {
            schedule = new Schedule
            {
                DoctorId = model.DoctorId,
                SlotDate = model.ScheduledDate,
                StartTime = model.ScheduledTime,
                EndTime = model.ScheduledTime.Add(TimeSpan.FromMinutes(30)), // Default 30 min slots
                IsAvailable = false
            };
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
        }
        else
        {
            schedule.IsAvailable = false;
        }

        var appointment = new Appointment
        {
            PatientId = model.PatientId,
            DoctorId = model.DoctorId,
            ScheduleId = schedule.ScheduleId,
            ScheduledTime = scheduledDateTime,
            Reason = model.Reason,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Appointment created successfully", appointmentId = appointment.AppointmentId });
    }

    // Get appointment details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Visits)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appointment == null)
            return NotFound();

        var visit = appointment.Visits.FirstOrDefault();

        var viewModel = new AppointmentDetailsViewModel
        {
            AppointmentId = appointment.AppointmentId,
            PatientName = appointment.Patient?.FullName ?? "",
            PatientPhone = appointment.Patient?.Phone ?? "",
            DoctorName = appointment.Doctor?.FullName ?? "",
            ScheduledTime = appointment.ScheduledTime ?? DateTime.MinValue,
            Reason = appointment.Reason ?? "",
            Status = appointment.Status ?? "",
            CreatedAt = appointment.CreatedAt ?? DateTime.MinValue,
            VisitId = visit?.VisitId,
            VisitTime = visit?.VisitTime,
            Diagnosis = visit?.Diagnosis
        };

        return View(viewModel);
    }

    // Edit appointment
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appointment == null)
            return NotFound();

        var viewModel = new EditAppointmentViewModel
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId ?? 0,
            DoctorId = appointment.DoctorId ?? 0,
            ScheduledDate = appointment.ScheduledTime?.Date ?? DateTime.Today,
            ScheduledTime = appointment.ScheduledTime?.TimeOfDay ?? TimeSpan.Zero,
            Reason = appointment.Reason ?? "",
            Status = appointment.Status ?? "",
            PatientName = appointment.Patient?.FullName ?? "",
            DoctorName = appointment.Doctor?.FullName ?? ""
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditAppointmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
        if (appointment == null)
            return Json(new { success = false, message = "Appointment not found" });

        var scheduledDateTime = model.ScheduledDate.Date.Add(model.ScheduledTime);

        // Check for conflicts (excluding current appointment)
        var conflict = await _context.Appointments
            .AnyAsync(a => a.AppointmentId != model.AppointmentId &&
                          a.DoctorId == model.DoctorId &&
                          a.ScheduledTime == scheduledDateTime &&
                          a.Status != "Cancelled");

        if (conflict)
        {
            return Json(new { success = false, message = "Time slot already booked" });
        }

        appointment.ScheduledTime = scheduledDateTime;
        appointment.Reason = model.Reason;
        appointment.Status = model.Status;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Appointment updated successfully" });
    }

    // Cancel appointment
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return Json(new { success = false, message = "Appointment not found" });

        appointment.Status = "Cancelled";

        // Free up the schedule slot
        if (appointment.ScheduleId.HasValue)
        {
            var schedule = await _context.Schedules.FindAsync(appointment.ScheduleId.Value);
            if (schedule != null)
                schedule.IsAvailable = true;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Appointment cancelled successfully" });
    }

    // Get available slots for a doctor on a date
    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(int doctorId, DateTime date)
    {
        var schedules = await _context.Schedules
            .Where(s => s.DoctorId == doctorId &&
                       s.SlotDate.HasValue &&
                       s.SlotDate.Value.Date == date.Date &&
                       s.IsAvailable == true)
            .OrderBy(s => s.StartTime)
            .Select(s => new TimeSlotDto
            {
                ScheduleId = s.ScheduleId,
                StartTime = s.StartTime ?? TimeSpan.Zero,
                EndTime = s.EndTime ?? TimeSpan.Zero,
                DisplayTime = s.StartTime!.Value.ToString(@"hh\:mm") + " - " + s.EndTime!.Value.ToString(@"hh\:mm")
            })
            .ToListAsync();

        return Json(schedules);
    }

    // Create schedule slots for a doctor
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        // Check if schedule already exists
        var existing = await _context.Schedules
            .AnyAsync(s => s.DoctorId == model.DoctorId &&
                          s.SlotDate.HasValue &&
                          s.SlotDate.Value.Date == model.SlotDate.Date &&
                          s.StartTime == model.StartTime);

        if (existing)
        {
            return Json(new { success = false, message = "Schedule slot already exists" });
        }

        var schedule = new Schedule
        {
            DoctorId = model.DoctorId,
            SlotDate = model.SlotDate,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            IsAvailable = model.IsAvailable
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Schedule created successfully", scheduleId = schedule.ScheduleId });
    }

    // Calendar view
    [HttpGet]
    public async Task<IActionResult> Calendar()
    {
        return View();
    }

    // Get calendar events
    [HttpGet]
    public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.ScheduledTime.HasValue &&
                       a.ScheduledTime.Value >= start &&
                       a.ScheduledTime.Value <= end);

        // Filter by role
        if (userRole == "Patient")
        {
            var patientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
            query = query.Where(a => a.PatientId == patientId);
        }
        else if (userRole == "Doctor")
        {
            query = query.Where(a => a.DoctorId == userId);
        }

        var events = await query
            .Select(a => new CalendarEventDto
            {
                AppointmentId = a.AppointmentId,
                Title = a.Patient!.FullName + " - " + a.Doctor!.FullName,
                Start = a.ScheduledTime!.Value,
                End = a.ScheduledTime.Value.AddMinutes(30),
                Status = a.Status ?? "",
                Color = a.Status == "Scheduled" ? "#007bff" :
                       a.Status == "Completed" ? "#28a745" :
                       a.Status == "Cancelled" ? "#dc3545" : "#6c757d"
            })
            .ToListAsync();

        return Json(events);
    }

    // AJAX: Search patients
    [HttpGet]
    public async Task<IActionResult> SearchPatients(string term)
    {
        var patients = await _context.Patients
            .Where(p => p.FullName!.Contains(term) ||
                       p.Phone!.Contains(term) ||
                       p.Cnic!.Contains(term))
            .Take(10)
            .Select(p => new
            {
                id = p.PatientId,
                text = p.FullName + " (" + p.Cnic + ")",
                phone = p.Phone
            })
            .ToListAsync();

        return Json(patients);
    }

    // AJAX: Get doctor schedule for date
    [HttpGet]
    public async Task<IActionResult> GetDoctorSchedule(int doctorId, DateTime date)
    {
        var schedules = await _context.Schedules
            .Where(s => s.DoctorId == doctorId &&
                       s.SlotDate.HasValue &&
                       s.SlotDate.Value.Date == date.Date)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.ScheduleId,
                s.StartTime,
                s.EndTime,
                s.IsAvailable,
                HasAppointment = _context.Appointments.Any(a => a.ScheduleId == s.ScheduleId && a.Status != "Cancelled")
            })
            .ToListAsync();

        return Json(schedules);
    }
}
