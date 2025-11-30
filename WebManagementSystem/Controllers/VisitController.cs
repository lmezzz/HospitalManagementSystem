using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize]
public class VisitController : Controller
{
    private readonly HmsContext _context;

    public VisitController(HmsContext context)
    {
        _context = context;
    }

    // List visits
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var query = _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .AsQueryable();

        // Role-based filtering
        if (userRole == "Doctor")
            query = query.Where(v => v.DoctorId == userId);
        else if (userRole == "Patient")
        {
            var patientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
            query = query.Where(v => v.PatientId == patientId);
        }

        var totalCount = await query.CountAsync();

        var visits = await query
            .OrderByDescending(v => v.VisitTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VisitDto
            {
                VisitId = v.VisitId,
                AppointmentId = v.AppointmentId,
                PatientId = v.PatientId,
                PatientName = v.Patient!.FullName ?? "",
                DoctorId = v.DoctorId,
                DoctorName = v.Doctor!.FullName ?? "",
                VisitTime = v.VisitTime,
                Symptoms = v.Symptoms,
                Diagnosis = v.Diagnosis,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        var viewModel = new VisitListViewModel
        {
            Visits = visits,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    // Create visit (usually from appointment)
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(int? appointmentId)
    {
        var viewModel = new CreateVisitViewModel
        {
            VisitTime = DateTime.Now
        };

        if (appointmentId.HasValue)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId.Value);

            if (appointment != null)
            {
                viewModel.AppointmentId = appointment.AppointmentId;
                viewModel.PatientId = appointment.PatientId ?? 0;
                viewModel.DoctorId = appointment.DoctorId ?? 0;
                viewModel.PatientName = appointment.Patient?.FullName ?? "";
                viewModel.DoctorName = appointment.Doctor?.FullName ?? "";
                viewModel.ChiefComplaint = appointment.Reason ?? "";
            }
        }
        else
        {
            // For walk-ins or manual entry
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var doctor = await _context.AppUsers.FindAsync(userId);
            if (doctor != null)
            {
                viewModel.DoctorId = userId;
                viewModel.DoctorName = doctor.FullName ?? "";
            }
        }

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(CreateVisitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            // Reload view data
            if (model.PatientId > 0)
            {
                var patient = await _context.Patients.FindAsync(model.PatientId);
                if (patient != null)
                    model.PatientName = patient.FullName ?? "";
            }
            if (model.DoctorId > 0)
            {
                var doctor = await _context.AppUsers.FindAsync(model.DoctorId);
                if (doctor != null)
                    model.DoctorName = doctor.FullName ?? "";
            }
            return View(model);
        }

        var visit = new Visit
        {
            AppointmentId = model.AppointmentId,
            PatientId = model.PatientId,
            DoctorId = model.DoctorId,
            VisitTime = model.VisitTime,
            Symptoms = model.Symptoms,
            Diagnosis = model.Diagnosis,
            Notes = model.Notes,
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Visits.Add(visit);

        // Update appointment status if linked
        if (model.AppointmentId.HasValue)
        {
            var appointment = await _context.Appointments.FindAsync(model.AppointmentId.Value);
            if (appointment != null)
                appointment.Status = "Completed";
        }

        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Visit #{visit.VisitId} created successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error saving visit to database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Details", new { id = visit.VisitId });
    }

    // Visit details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.Prescriptions)
            .ThenInclude(p => p.PrescriptionItems)
            .Include(v => v.LabOrders)
            .ThenInclude(l => l.LabTest)
            .FirstOrDefaultAsync(v => v.VisitId == id);

        if (visit == null)
            return NotFound();

        var viewModel = new VisitDetailsViewModel
        {
            VisitId = visit.VisitId,
            AppointmentId = visit.AppointmentId,
            PatientId = visit.PatientId ?? 0,
            PatientName = visit.Patient?.FullName ?? "",
            PatientAge = visit.Patient?.DateOfBirth.HasValue == true
                ? DateTime.Today.Year - visit.Patient.DateOfBirth.Value.Year
                : null,
            PatientGender = visit.Patient?.Gender ?? "",
            DoctorId = visit.DoctorId ?? 0,
            DoctorName = visit.Doctor?.FullName ?? "",
            VisitTime = visit.VisitTime ?? DateTime.MinValue,
            Symptoms = visit.Symptoms ?? "",
            Diagnosis = visit.Diagnosis ?? "",
            Notes = visit.Notes ?? "",
            CreatedAt = visit.CreatedAt ?? DateTime.MinValue
        };

        // Prescriptions summary
        viewModel.Prescriptions = visit.Prescriptions.Select(p => new PrescriptionSummaryDto
        {
            PrescriptionId = p.PrescriptionId,
            CreatedAt = p.CreatedAt ?? DateTime.MinValue,
            ItemCount = p.PrescriptionItems.Count,
            Status = "Pending" // Status determined by prescription dispensing
        }).ToList();

        // Lab orders summary
        viewModel.LabOrders = visit.LabOrders.Select(l => new LabOrderSummaryDto
        {
            LabOrderId = l.LabOrderId,
            TestName = l.LabTest?.TestName ?? "",
            Status = l.Status ?? "",
            OrderTime = l.OrderTime ?? DateTime.MinValue
        }).ToList();

        return View(viewModel);
    }

    // Edit visit
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstOrDefaultAsync(v => v.VisitId == id);

        if (visit == null)
            return NotFound();

        var viewModel = new EditVisitViewModel
        {
            VisitId = visit.VisitId,
            AppointmentId = visit.AppointmentId,
            PatientId = visit.PatientId ?? 0,
            DoctorId = visit.DoctorId ?? 0,
            VisitTime = visit.VisitTime ?? DateTime.Now,
            Symptoms = visit.Symptoms ?? "",
            Diagnosis = visit.Diagnosis ?? "",
            Notes = visit.Notes ?? "",
            PatientName = visit.Patient?.FullName ?? "",
            DoctorName = visit.Doctor?.FullName ?? ""
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(EditVisitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return View(model);
        }

        var visit = await _context.Visits.FindAsync(model.VisitId);
        if (visit == null)
        {
            TempData["ErrorMessage"] = "Visit not found";
            return RedirectToAction("Index");
        }

        visit.VisitTime = model.VisitTime;
        visit.Symptoms = model.Symptoms;
        visit.Diagnosis = model.Diagnosis;
        visit.Notes = model.Notes;

        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Visit #{model.VisitId} updated successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error updating visit in database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Details", new { id = visit.VisitId });
    }

    // Patient medical history
    [HttpGet]
    public async Task<IActionResult> PatientHistory(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound();

        var visitHistory = await _context.Visits
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitTime)
            .Select(v => new VisitHistoryDto
            {
                VisitId = v.VisitId,
                VisitTime = v.VisitTime ?? DateTime.MinValue,
                DoctorName = v.Doctor!.FullName ?? "",
                Diagnosis = v.Diagnosis ?? "",
                Symptoms = v.Symptoms ?? ""
            })
            .ToListAsync();

        var prescriptionHistory = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionHistoryDto
            {
                PrescriptionId = p.PrescriptionId,
                CreatedAt = p.CreatedAt ?? DateTime.MinValue,
                DoctorName = p.Doctor!.FullName ?? "",
                Medications = p.PrescriptionItems.Select(i => i.Medication!.Name ?? "").ToList()
            })
            .ToListAsync();

        var labHistory = await _context.LabOrders
            .Include(l => l.LabTest)
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.OrderTime)
            .Select(l => new LabHistoryDto
            {
                LabOrderId = l.LabOrderId,
                TestName = l.LabTest!.TestName ?? "",
                OrderTime = l.OrderTime ?? DateTime.MinValue,
                Status = l.Status ?? "",
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        var viewModel = new PatientMedicalHistoryViewModel
        {
            PatientId = patientId,
            PatientName = patient.FullName ?? "",
            DateOfBirth = patient.DateOfBirth.HasValue ? patient.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            Age = patient.DateOfBirth.HasValue
                ? DateTime.Today.Year - patient.DateOfBirth.Value.Year
                : null,
            Gender = patient.Gender ?? "",
            Allergies = patient.Allergies,
            ChronicConditions = patient.ChronicConditions,
            VisitHistory = visitHistory,
            PrescriptionHistory = prescriptionHistory,
            LabHistory = labHistory
        };

        return View(viewModel);
    }

    // AJAX: Quick visit from appointment
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> StartVisitFromAppointment(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Visits)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found";
            return RedirectToAction("Index", "Appointment");
        }

        // Check if visit already exists
        if (appointment.Visits.Any())
        {
            TempData["InfoMessage"] = "Visit already exists for this appointment";
            return RedirectToAction("Details", new { id = appointment.Visits.First().VisitId });
        }

        var visit = new Visit
        {
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            VisitTime = DateTime.Now,
            Symptoms = appointment.Reason,
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Visits.Add(visit);

        appointment.Status = "InProgress";

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Visit started successfully";
        return RedirectToAction("Details", new { id = visit.VisitId });
    }

    // AJAX: Get patient visits
    [HttpGet]
    public async Task<IActionResult> GetPatientVisits(int patientId, int take = 5)
    {
        var visits = await _context.Visits
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitTime)
            .Take(take)
            .Select(v => new
            {
                v.VisitId,
                VisitTime = v.VisitTime!.Value.ToString("MMM dd, yyyy"),
                DoctorName = v.Doctor!.FullName,
                v.Diagnosis,
                v.Symptoms
            })
            .ToListAsync();

        return Json(visits);
    }
}
