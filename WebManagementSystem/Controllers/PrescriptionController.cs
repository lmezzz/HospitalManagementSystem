using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize]
public class PrescriptionController : Controller
{
    private readonly HmsContext _context;

    public PrescriptionController(HmsContext context)
    {
        _context = context;
    }

    // List prescriptions
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var query = _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .AsQueryable();

        // Role-based filtering
        if (userRole == "Doctor")
            query = query.Where(p => p.DoctorId == userId);
        else if (userRole == "Patient")
        {
            var patientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
            query = query.Where(p => p.Visit!.PatientId == patientId);
        }

        var totalCount = await query.CountAsync();

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                VisitId = p.VisitId,
                DoctorId = p.DoctorId,
                DoctorName = p.Doctor!.FullName ?? "",
                PatientName = p.Visit!.Patient!.FullName ?? "",
                CreatedAt = p.CreatedAt,
                ItemCount = p.PrescriptionItems.Count,
                Status = "Pending" // Status determined by dispensing status
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

    // Create prescription
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(int visitId)
    {
        var visit = await _context.Visits
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstOrDefaultAsync(v => v.VisitId == visitId);

        if (visit == null)
            return NotFound();

        var viewModel = new CreatePrescriptionViewModel
        {
            VisitId = visitId,
            DoctorId = visit.DoctorId ?? 0,
            PatientId = visit.PatientId ?? 0,
            PatientName = visit.Patient?.FullName ?? "",
            DoctorName = visit.Doctor?.FullName ?? ""
        };

        // Get available medications
        viewModel.AvailableMedications = await _context.Medications
            .Where(m => m.StockQuantity > 0)
            .Select(m => new MedicationSelectDto
            {
                MedicationId = m.MedicationId,
                Name = m.Name ?? "",
                Description = m.Description ?? "",
                UnitPrice = m.UnitPrice ?? 0,
                StockQuantity = m.StockQuantity ?? 0
            })
            .ToListAsync();

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(CreatePrescriptionViewModel model)
    {
        if (!ModelState.IsValid || !model.Items.Any())
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            if (!model.Items.Any())
                errors = string.IsNullOrEmpty(errors) ? "Please add at least one medication" : errors + ". Please add at least one medication";
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            // Reload view data
            if (model.VisitId > 0)
            {
                var visit = await _context.Visits
                    .Include(v => v.Patient)
                    .Include(v => v.Doctor)
                    .FirstOrDefaultAsync(v => v.VisitId == model.VisitId);
                if (visit != null)
                {
                    model.PatientName = visit.Patient?.FullName ?? "";
                    model.DoctorName = visit.Doctor?.FullName ?? "";
                }
            }
            model.AvailableMedications = await _context.Medications
                .Where(m => m.StockQuantity > 0)
                .Select(m => new MedicationSelectDto
                {
                    MedicationId = m.MedicationId,
                    Name = m.Name ?? "",
                    Description = m.Description ?? "",
                    UnitPrice = m.UnitPrice ?? 0,
                    StockQuantity = m.StockQuantity ?? 0
                })
                .ToListAsync();
            return View(model);
        }

        var prescription = new Prescription
        {
            VisitId = model.VisitId,
            DoctorId = model.DoctorId,
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        // Add prescription items
        foreach (var item in model.Items)
        {
            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescription.PrescriptionId,
                MedicationId = item.MedicationId,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Quantity = item.Quantity
            };
            _context.PrescriptionItems.Add(prescriptionItem);
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Prescription #{prescription.PrescriptionId} created successfully! {model.Items.Count} medication(s) added. Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error saving prescription to database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Details", new { id = prescription.PrescriptionId });
    }

    // Prescription details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
            return NotFound();

        var viewModel = new PrescriptionDetailsViewModel
        {
            PrescriptionId = prescription.PrescriptionId,
            VisitId = prescription.VisitId,
            PatientId = prescription.Visit?.PatientId ?? 0,
            PatientName = prescription.Visit?.Patient?.FullName ?? "",
            PatientAge = prescription.Visit?.Patient?.DateOfBirth.HasValue == true
                ? DateTime.Today.Year - prescription.Visit.Patient.DateOfBirth.Value.Year
                : null,
            PatientGender = prescription.Visit?.Patient?.Gender ?? "",
            DoctorId = prescription.DoctorId ?? 0,
            DoctorName = prescription.Doctor?.FullName ?? "",
            CreatedAt = prescription.CreatedAt ?? DateTime.MinValue,
            VisitDate = prescription.Visit?.VisitTime,
            Diagnosis = prescription.Visit?.Diagnosis,
            Status = "Pending" // TODO: Check dispensing status
        };

        viewModel.Items = prescription.PrescriptionItems.Select(i => new PrescriptionItemDetailDto
        {
            PrescriptionItemId = i.PrescriptionItemId,
            MedicationName = i.Medication?.Name ?? "",
            Dosage = i.Dosage ?? "",
            Frequency = i.Frequency ?? "",
            Duration = i.Duration ?? "",
            Quantity = i.Quantity ?? 0,
            UnitPrice = i.Medication?.UnitPrice ?? 0,
            TotalPrice = (i.Quantity ?? 0) * (i.Medication?.UnitPrice ?? 0),
            IsDispensed = false // TODO: Check dispensing status
        }).ToList();

        return View(viewModel);
    }

    // Edit prescription
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
            return NotFound();

        var viewModel = new EditPrescriptionViewModel
        {
            PrescriptionId = prescription.PrescriptionId,
            VisitId = prescription.VisitId ?? 0,
            DoctorId = prescription.DoctorId ?? 0,
            PatientId = prescription.Visit?.PatientId ?? 0,
            PatientName = prescription.Visit?.Patient?.FullName ?? "",
            DoctorName = prescription.Doctor?.FullName ?? "",
            CreatedAt = prescription.CreatedAt ?? DateTime.MinValue
        };

        viewModel.Items = prescription.PrescriptionItems.Select(i => new PrescriptionItemViewModel
        {
            PrescriptionItemId = i.PrescriptionItemId,
            MedicationId = i.MedicationId ?? 0,
            MedicationName = i.Medication?.Name ?? "",
            Dosage = i.Dosage ?? "",
            Frequency = i.Frequency ?? "",
            Duration = i.Duration ?? "",
            Quantity = i.Quantity ?? 0
        }).ToList();

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(EditPrescriptionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(p => p.PrescriptionId == model.PrescriptionId);

        if (prescription == null)
            return Json(new { success = false, message = "Prescription not found" });

        // Remove old items
        _context.PrescriptionItems.RemoveRange(prescription.PrescriptionItems);

        // Add new items
        foreach (var item in model.Items)
        {
            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescription.PrescriptionId,
                MedicationId = item.MedicationId,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Quantity = item.Quantity
            };
            _context.PrescriptionItems.Add(prescriptionItem);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Prescription updated successfully" });
    }

    // Delete prescription
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Delete(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
        {
            TempData["ErrorMessage"] = "Prescription not found";
            return RedirectToAction("Index");
        }

        _context.PrescriptionItems.RemoveRange(prescription.PrescriptionItems);
        _context.Prescriptions.Remove(prescription);

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Prescription #{id} deleted successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting prescription from database: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    // Print prescription
    [HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
            return NotFound();

        var viewModel = new PrescriptionDetailsViewModel
        {
            PrescriptionId = prescription.PrescriptionId,
            VisitId = prescription.VisitId,
            PatientId = prescription.Visit?.PatientId ?? 0,
            PatientName = prescription.Visit?.Patient?.FullName ?? "",
            PatientAge = prescription.Visit?.Patient?.DateOfBirth.HasValue == true
                ? DateTime.Today.Year - prescription.Visit.Patient.DateOfBirth.Value.Year
                : null,
            PatientGender = prescription.Visit?.Patient?.Gender ?? "",
            DoctorId = prescription.DoctorId ?? 0,
            DoctorName = prescription.Doctor?.FullName ?? "",
            CreatedAt = prescription.CreatedAt ?? DateTime.MinValue,
            VisitDate = prescription.Visit?.VisitTime,
            Diagnosis = prescription.Visit?.Diagnosis
        };

        viewModel.Items = prescription.PrescriptionItems.Select(i => new PrescriptionItemDetailDto
        {
            PrescriptionItemId = i.PrescriptionItemId,
            MedicationName = i.Medication?.Name ?? "",
            Dosage = i.Dosage ?? "",
            Frequency = i.Frequency ?? "",
            Duration = i.Duration ?? "",
            Quantity = i.Quantity ?? 0,
            Instructions = $"{i.Dosage} - {i.Frequency} for {i.Duration}"
        }).ToList();

        return View(viewModel);
    }

    // AJAX: Get medications
    [HttpGet]
    public async Task<IActionResult> SearchMedications(string term)
    {
        var medications = await _context.Medications
            .Where(m => m.Name!.Contains(term) && m.StockQuantity > 0)
            .Take(10)
            .Select(m => new
            {
                id = m.MedicationId,
                text = m.Name,
                price = m.UnitPrice,
                stock = m.StockQuantity,
                description = m.Description
            })
            .ToListAsync();

        return Json(medications);
    }

    // AJAX: Get prescription for dispensing
    [HttpGet]
    public async Task<IActionResult> GetForDispensing(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
            return Json(new { success = false, message = "Prescription not found" });

        var result = new
        {
            success = true,
            prescriptionId = prescription.PrescriptionId,
            patientName = prescription.Visit?.Patient?.FullName,
            doctorName = prescription.Doctor?.FullName,
            createdAt = prescription.CreatedAt?.ToString("MMM dd, yyyy"),
            items = prescription.PrescriptionItems.Select(i => new
            {
                itemId = i.PrescriptionItemId,
                medicationId = i.MedicationId,
                medicationName = i.Medication?.Name,
                quantity = i.Quantity,
                availableStock = i.Medication?.StockQuantity,
                unitPrice = i.Medication?.UnitPrice,
                totalPrice = (i.Quantity ?? 0) * (i.Medication?.UnitPrice ?? 0),
                dosage = i.Dosage,
                frequency = i.Frequency,
                canDispense = (i.Medication?.StockQuantity ?? 0) >= (i.Quantity ?? 0)
            }).ToList()
        };

        return Json(result);
    }

    // AJAX: Get patient prescriptions
    [HttpGet]
    public async Task<IActionResult> GetPatientPrescriptions(int patientId, int take = 5)
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.PrescriptionItems)
            .Include(p => p.Visit)
            .Where(p => p.Visit!.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .Select(p => new
            {
                p.PrescriptionId,
                CreatedAt = p.CreatedAt!.Value.ToString("MMM dd, yyyy"),
                DoctorName = p.Doctor!.FullName,
                ItemCount = p.PrescriptionItems.Count,
                Medications = p.PrescriptionItems.Select(i => i.Medication!.Name).ToList()
            })
            .ToListAsync();

        return Json(prescriptions);
    }
}
