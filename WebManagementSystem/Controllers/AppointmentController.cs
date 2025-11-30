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
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            
            // Reload view data
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Patient")
            {
                model.AvailablePatients = await _context.Patients
                    .Select(p => new PatientSelectDto
                    {
                        PatientId = p.PatientId,
                        FullName = p.FullName ?? "",
                        Phone = p.Phone ?? "",
                        CNIC = p.Cnic ?? ""
                    })
                    .ToListAsync();
            }
            model.AvailableDoctors = await _context.AppUsers
                .Where(u => u.RoleId == 3 && u.IsActive == true) // Doctor role
                .Select(u => new DoctorSelectDto
                {
                    DoctorId = u.UserId,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? ""
                })
                .ToListAsync();
            return View(model);
        }

        // Get the selected schedule slot
        if (!model.ScheduleId.HasValue)
        {
            TempData["ErrorMessage"] = "Please select a time slot.";
            
            // Reload view data
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Patient")
            {
                model.AvailablePatients = await _context.Patients
                    .Select(p => new PatientSelectDto
                    {
                        PatientId = p.PatientId,
                        FullName = p.FullName ?? "",
                        Phone = p.Phone ?? "",
                        CNIC = p.Cnic ?? ""
                    })
                    .ToListAsync();
            }
            model.AvailableDoctors = await _context.AppUsers
                .Where(u => u.RoleId == 3 && u.IsActive == true)
                .Select(u => new DoctorSelectDto
                {
                    DoctorId = u.UserId,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? ""
                })
                .ToListAsync();
            return View(model);
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(s => s.ScheduleId == model.ScheduleId.Value &&
                                     s.DoctorId == model.DoctorId &&
                                     s.IsAvailable == true);

        if (schedule == null)
        {
            TempData["ErrorMessage"] = "Selected time slot is no longer available. Please select another slot.";
            
            // Reload view data
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Patient")
            {
                model.AvailablePatients = await _context.Patients
                    .Select(p => new PatientSelectDto
                    {
                        PatientId = p.PatientId,
                        FullName = p.FullName ?? "",
                        Phone = p.Phone ?? "",
                        CNIC = p.Cnic ?? ""
                    })
                    .ToListAsync();
            }
            model.AvailableDoctors = await _context.AppUsers
                .Where(u => u.RoleId == 3 && u.IsActive == true)
                .Select(u => new DoctorSelectDto
                {
                    DoctorId = u.UserId,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? ""
                })
                .ToListAsync();
            return View(model);
        }

        // Check if slot is already booked
        var existingAppointment = await _context.Appointments
            .AnyAsync(a => a.ScheduleId == model.ScheduleId.Value &&
                          a.Status != "Cancelled");

        if (existingAppointment)
        {
            TempData["ErrorMessage"] = "This time slot is already booked. Please select another slot.";
            
            // Reload view data
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Patient")
            {
                model.AvailablePatients = await _context.Patients
                    .Select(p => new PatientSelectDto
                    {
                        PatientId = p.PatientId,
                        FullName = p.FullName ?? "",
                        Phone = p.Phone ?? "",
                        CNIC = p.Cnic ?? ""
                    })
                    .ToListAsync();
            }
            model.AvailableDoctors = await _context.AppUsers
                .Where(u => u.RoleId == 3 && u.IsActive == true)
                .Select(u => new DoctorSelectDto
                {
                    DoctorId = u.UserId,
                    FullName = u.FullName ?? "",
                    Email = u.Email ?? ""
                })
                .ToListAsync();
            return View(model);
        }

        // Combine date and time from schedule
        var scheduledDateTime = schedule.SlotDate.ToDateTime(schedule.StartTime);

        // Mark schedule as unavailable
        schedule.IsAvailable = false;

        var appointment = new Appointment
        {
            PatientId = model.PatientId,
            DoctorId = model.DoctorId,
            ScheduleId = schedule.ScheduleId,
            ScheduledTime = scheduledDateTime,
            Reason = model.Reason,
            Status = "Scheduled",
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Appointments.Add(appointment);
        
        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Appointment created successfully! Appointment ID: {appointment.AppointmentId}. Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error saving appointment to database: {ex.Message}";
            return RedirectToAction("Create");
        }
        
        return RedirectToAction("Index");
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
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return View(model);
        }

        var appointment = await _context.Appointments.FindAsync(model.AppointmentId);
        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found";
            return RedirectToAction("Index");
        }

        var scheduledDateTime = model.ScheduledDate.Date.Add(model.ScheduledTime);

        // Check for conflicts (excluding current appointment)
        var conflict = await _context.Appointments
            .AnyAsync(a => a.AppointmentId != model.AppointmentId &&
                          a.DoctorId == model.DoctorId &&
                          a.ScheduledTime == scheduledDateTime &&
                          a.Status != "Cancelled");

        if (conflict)
        {
            TempData["ErrorMessage"] = "Time slot already booked. Please select a different time.";
            return View(model);
        }

        appointment.ScheduledTime = scheduledDateTime;
        appointment.Reason = model.Reason;
        appointment.Status = model.Status;

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Appointment #{model.AppointmentId} updated successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error updating appointment in database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Index");
    }

    // Cancel appointment
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found";
            return RedirectToAction("Index");
        }

        appointment.Status = "Cancelled";

        // Free up the schedule slot
        if (appointment.ScheduleId.HasValue)
        {
            var schedule = await _context.Schedules.FindAsync(appointment.ScheduleId.Value);
            if (schedule != null)
                schedule.IsAvailable = true;
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Appointment #{id} cancelled successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error cancelling appointment in database: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // Get available slots for a doctor on a date
    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(int doctorId, DateTime date)
    {
        var scheduleDate = DateOnly.FromDateTime(date);
        
        await EnsureDefaultScheduleAsync(doctorId, scheduleDate);

        // Get all schedules for the doctor on this date
        var allSchedules = await _context.Schedules
            .Where(s => s.DoctorId == doctorId &&
                       s.SlotDate == scheduleDate)
            .ToListAsync();

        // Get appointment IDs for booked slots
        var bookedScheduleIds = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                       a.ScheduledTime.HasValue &&
                       DateOnly.FromDateTime(a.ScheduledTime.Value) == scheduleDate &&
                       a.Status != "Cancelled")
            .Select(a => a.ScheduleId)
            .ToListAsync();

        // Filter available slots (IsAvailable = true AND not booked)
        var availableSlots = allSchedules
            .Where(s => s.IsAvailable == true && 
                       !bookedScheduleIds.Contains(s.ScheduleId))
            .OrderBy(s => s.StartTime)
            .Select(s => new TimeSlotDto
            {
                ScheduleId = s.ScheduleId,
                StartTime = new TimeSpan(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second),
                EndTime = new TimeSpan(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second),
                DisplayTime = s.StartTime.ToString(@"HH\:mm") + " - " + s.EndTime.ToString(@"HH\:mm")
            })
            .ToList();

        return Json(availableSlots);
    }

    // Create schedule slots for a doctor
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return RedirectToAction("Calendar");
        }

        // Check if schedule already exists
        var scheduleDate = DateOnly.FromDateTime(model.SlotDate);
        var scheduleStartTime = TimeOnly.FromTimeSpan(model.StartTime);
        
        var existing = await _context.Schedules
            .AnyAsync(s => s.DoctorId == model.DoctorId &&
                          s.SlotDate == scheduleDate &&
                          s.StartTime == scheduleStartTime);

        if (existing)
        {
            TempData["ErrorMessage"] = "Schedule slot already exists for this doctor at this time.";
            return RedirectToAction("Calendar");
        }

        var schedule = new Schedule
        {
            DoctorId = model.DoctorId,
            SlotDate = scheduleDate,
            StartTime = scheduleStartTime,
            EndTime = TimeOnly.FromTimeSpan(model.EndTime),
            IsAvailable = model.IsAvailable
        };

        _context.Schedules.Add(schedule);
        
        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Schedule slot created successfully! Schedule ID: {schedule.ScheduleId}. Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error creating schedule in database: {ex.Message}";
            return RedirectToAction("Calendar");
        }

        return RedirectToAction("Calendar");
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
        var scheduleDate = DateOnly.FromDateTime(date);

        await EnsureDefaultScheduleAsync(doctorId, scheduleDate);
        
        var schedules = await _context.Schedules
            .Where(s => s.DoctorId == doctorId &&
                       s.SlotDate == scheduleDate)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.ScheduleId,
                StartTime = new TimeSpan(s.StartTime.Hour, s.StartTime.Minute, s.StartTime.Second),
                EndTime = new TimeSpan(s.EndTime.Hour, s.EndTime.Minute, s.EndTime.Second),
                s.IsAvailable,
                HasAppointment = _context.Appointments.Any(a => a.ScheduleId == s.ScheduleId && a.Status != "Cancelled")
            })
            .ToListAsync();

        return Json(schedules);
    }

    private async Task EnsureDefaultScheduleAsync(int doctorId, DateOnly slotDate)
    {
        var hasSchedules = await _context.Schedules
            .AnyAsync(s => s.DoctorId == doctorId && s.SlotDate == slotDate);

        if (hasSchedules)
            return;

        var defaultSlots = new List<Schedule>();
        var openingTime = new TimeOnly(9, 0);
        var closingTime = new TimeOnly(17, 0);
        var slotDuration = TimeSpan.FromMinutes(30);

        var currentStart = openingTime;
        while (currentStart < closingTime)
        {
            var currentEnd = currentStart.Add(slotDuration);
            if (currentEnd > closingTime)
                break;

            defaultSlots.Add(new Schedule
            {
                DoctorId = doctorId,
                SlotDate = slotDate,
                StartTime = currentStart,
                EndTime = currentEnd,
                IsAvailable = true
            });

            currentStart = currentEnd;
        }

        if (defaultSlots.Count == 0)
            return;

        _context.Schedules.AddRange(defaultSlots);
        await _context.SaveChangesAsync();
    }
}
