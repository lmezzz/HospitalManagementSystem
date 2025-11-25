using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize(Roles = "Patient")]
public class PatientController : Controller
{
    private readonly HmsContext _context;

    public PatientController(HmsContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var viewModel = new PatientDashboardViewModel
        {
            PatientName = patient.FullName ?? "",
            UpcomingAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == patient.PatientId &&
                               a.ScheduledTime.HasValue &&
                               a.ScheduledTime >= DateTime.Now &&
                               a.Status == "Scheduled"),
            PendingLabResults = await _context.LabOrders
                .CountAsync(l => l.PatientId == patient.PatientId &&
                               (l.Status == "Pending" || l.Status == "InProgress"))
        };

        // Calculate outstanding bills
        var bills = await _context.Bills
            .Include(b => b.Payments)
            .Where(b => b.PatientId == patient.PatientId)
            .ToListAsync();

        viewModel.OutstandingBillAmount = bills
            .Sum(b => (b.TotalAmount ?? 0) - b.Payments.Sum(p => p.AmountPaid ?? 0));

        // Upcoming appointments
        viewModel.Appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patient.PatientId &&
                       a.ScheduledTime >= DateTime.Now &&
                       a.Status == "Scheduled")
            .OrderBy(a => a.ScheduledTime)
            .Take(5)
            .Select(a => new UpcomingAppointmentDto
            {
                AppointmentId = a.AppointmentId,
                ScheduledTime = a.ScheduledTime!.Value,
                DoctorName = a.Doctor!.FullName ?? "",
                Reason = a.Reason ?? "",
                Status = a.Status ?? ""
            })
            .ToListAsync();

        // Recent prescriptions
        viewModel.RecentPrescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patient.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                CreatedAt = p.CreatedAt!.Value,
                DoctorName = p.Doctor!.FullName ?? "",
                ItemCount = p.PrescriptionItems.Count,
                Status = "Pending"
            })
            .ToListAsync();

        // Pending bills
        viewModel.PendingBills = await _context.Bills
            .Include(b => b.Payments)
            .Where(b => b.PatientId == patient.PatientId &&
                       (b.Status == "Unpaid" || b.Status == "Partial"))
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new PendingBillDto
            {
                BillId = b.BillId,
                TotalAmount = b.TotalAmount ?? 0,
                AmountPaid = b.Payments.Sum(p => p.AmountPaid ?? 0),
                Balance = (b.TotalAmount ?? 0) - b.Payments.Sum(p => p.AmountPaid ?? 0),
                CreatedAt = b.CreatedAt!.Value
            })
            .ToListAsync();

        return View(viewModel);
    }

    public async Task<IActionResult> Appointments()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patient.PatientId)
            .OrderByDescending(a => a.ScheduledTime)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
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
            TotalCount = appointments.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> LabReports()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var labOrders = await _context.LabOrders
            .Include(l => l.Doctor)
            .Include(l => l.LabTest)
            .Include(l => l.LabResults)
            .Where(l => l.PatientId == patient.PatientId)
            .OrderByDescending(l => l.OrderTime)
            .Select(l => new LabOrderDto
            {
                LabOrderId = l.LabOrderId,
                DoctorName = l.Doctor!.FullName ?? "",
                TestName = l.LabTest!.TestName ?? "",
                OrderTime = l.OrderTime!.Value,
                Status = l.Status ?? "",
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        var viewModel = new LabOrderListViewModel
        {
            Orders = labOrders,
            TotalCount = labOrders.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Prescriptions()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patient.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                DoctorName = p.Doctor!.FullName ?? "",
                CreatedAt = p.CreatedAt,
                ItemCount = p.PrescriptionItems.Count,
                Status = "Pending"
            })
            .ToListAsync();

        var viewModel = new PrescriptionListViewModel
        {
            Prescriptions = prescriptions,
            TotalCount = prescriptions.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Billing()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var bills = await _context.Bills
            .Include(b => b.Payments)
            .Where(b => b.PatientId == patient.PatientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var billDtos = bills.Select(b => new BillDto
        {
            BillId = b.BillId,
            TotalAmount = b.TotalAmount ?? 0,
            AmountPaid = b.Payments.Sum(p => p.AmountPaid ?? 0),
            Balance = (b.TotalAmount ?? 0) - b.Payments.Sum(p => p.AmountPaid ?? 0),
            Status = b.Status ?? "",
            CreatedAt = b.CreatedAt!.Value
        }).ToList();

        var viewModel = new BillListViewModel
        {
            Bills = billDtos,
            TotalCount = billDtos.Count
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Pharmacy()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patient.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Prescriptions = prescriptions;

        return View();
    }

    // Medical history
    [HttpGet]
    public async Task<IActionResult> MedicalHistory()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var viewModel = new PatientMedicalHistoryViewModel
        {
            PatientId = patient.PatientId,
            PatientName = patient.FullName ?? "",
            DateOfBirth = patient.DateOfBirth.HasValue ? patient.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            Age = patient.DateOfBirth.HasValue
                ? DateTime.Today.Year - patient.DateOfBirth.Value.Year
                : null,
            Gender = patient.Gender ?? "",
            Allergies = patient.Allergies,
            ChronicConditions = patient.ChronicConditions
        };

        // Visit history
        viewModel.VisitHistory = await _context.Visits
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patient.PatientId)
            .OrderByDescending(v => v.VisitTime)
            .Select(v => new VisitHistoryDto
            {
                VisitId = v.VisitId,
                VisitTime = v.VisitTime!.Value,
                DoctorName = v.Doctor!.FullName ?? "",
                Diagnosis = v.Diagnosis ?? "",
                Symptoms = v.Symptoms ?? ""
            })
            .ToListAsync();

        // Prescription history
        viewModel.PrescriptionHistory = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patient.PatientId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PrescriptionHistoryDto
            {
                PrescriptionId = p.PrescriptionId,
                CreatedAt = p.CreatedAt!.Value,
                DoctorName = p.Doctor!.FullName ?? "",
                Medications = p.PrescriptionItems.Select(i => i.Medication!.Name ?? "").ToList()
            })
            .ToListAsync();

        // Lab history
        viewModel.LabHistory = await _context.LabOrders
            .Include(l => l.LabTest)
            .Where(l => l.PatientId == patient.PatientId)
            .OrderByDescending(l => l.OrderTime)
            .Select(l => new LabHistoryDto
            {
                LabOrderId = l.LabOrderId,
                TestName = l.LabTest!.TestName ?? "",
                OrderTime = l.OrderTime!.Value,
                Status = l.Status ?? "",
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        return View(viewModel);
    }

    // Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return RedirectToAction("Login", "Account");

        var viewModel = new EditPatientViewModel
        {
            PatientId = patient.PatientId,
            FullName = patient.FullName ?? "",
            Gender = patient.Gender ?? "",
            DateOfBirth = patient.DateOfBirth ?? DateTime.Today,
            Phone = patient.Phone ?? "",
            CNIC = patient.Cnic ?? "",
            Address = patient.Address ?? "",
            Email = patient.User?.Email ?? "",
            Allergies = patient.Allergies,
            ChronicConditions = patient.ChronicConditions,
            EmergencyContact = patient.EmergencyContact
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(EditPatientViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var patient = await _context.Patients.FindAsync(model.PatientId);
        if (patient == null)
            return Json(new { success = false, message = "Patient not found" });

        patient.FullName = model.FullName;
        patient.Gender = model.Gender;
        patient.DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth);
        patient.Phone = model.Phone;
        patient.Address = model.Address;
        patient.Allergies = model.Allergies;
        patient.ChronicConditions = model.ChronicConditions;
        patient.EmergencyContact = model.EmergencyContact;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Profile updated successfully" });
    }
}
