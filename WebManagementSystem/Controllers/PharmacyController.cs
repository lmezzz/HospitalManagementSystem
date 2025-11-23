using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class PharmacyController : Controller
{
    private readonly HmsContext _context;

    public PharmacyController(HmsContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new PharmacyDashboardViewModel
        {
            PendingPrescriptions = await GetPendingPrescriptionsCount(),
            DispensedToday = await _context.Prescriptions
                .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value.Date == DateTime.Today)
                .CountAsync(),
            LowStockItems = await _context.Medications
                .CountAsync(m => m.StockQuantity <= m.LowStockThreshold)
        };

        // Pending prescriptions queue
        viewModel.PendingQueue = await GetPendingPrescriptionsQueue();

        // Low stock medications
        viewModel.LowStockMedications = await _context.Medications
            .Where(m => m.StockQuantity <= m.LowStockThreshold)
            .OrderBy(m => m.StockQuantity)
            .Take(10)
            .Select(m => new LowStockMedicationDto
            {
                MedicationId = m.MedicationId,
                Name = m.Name ?? "",
                StockQuantity = m.StockQuantity ?? 0,
                LowStockThreshold = m.LowStockThreshold ?? 0
            })
            .ToListAsync();

        return View(viewModel);
    }

    // Issue Medicine (Dispense prescriptions)
    [HttpGet]
    public async Task<IActionResult> IssueMedicine(int? prescriptionId)
    {
        if (!prescriptionId.HasValue)
        {
            // Show pending prescriptions list
            var prescriptions = await GetPendingPrescriptionsQueue();
            return View(prescriptions);
        }

        var prescription = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value);

        if (prescription == null)
            return NotFound();

        var viewModel = new DispensePrescriptionViewModel
        {
            PrescriptionId = prescription.PrescriptionId,
            PatientName = prescription.Visit?.Patient?.FullName ?? "",
            DoctorName = prescription.Doctor?.FullName ?? "",
            CreatedAt = prescription.CreatedAt ?? DateTime.MinValue
        };

        viewModel.Items = prescription.PrescriptionItems.Select(i => new DispenseItemDto
        {
            PrescriptionItemId = i.PrescriptionItemId,
            MedicationId = i.MedicationId ?? 0,
            MedicationName = i.Medication?.Name ?? "",
            Quantity = i.Quantity ?? 0,
            AvailableStock = i.Medication?.StockQuantity ?? 0,
            UnitPrice = i.Medication?.UnitPrice ?? 0,
            TotalPrice = (i.Quantity ?? 0) * (i.Medication?.UnitPrice ?? 0),
            CanDispense = (i.Medication?.StockQuantity ?? 0) >= (i.Quantity ?? 0),
            Dosage = i.Dosage ?? "",
            Frequency = i.Frequency ?? ""
        }).ToList();

        viewModel.TotalAmount = viewModel.Items.Sum(i => i.TotalPrice);

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> DispenseMedication(int prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
            return Json(new { success = false, message = "Prescription not found" });

        // Check stock availability for all items
        foreach (var item in prescription.PrescriptionItems)
        {
            if ((item.Medication?.StockQuantity ?? 0) < (item.Quantity ?? 0))
            {
                return Json(new
                {
                    success = false,
                    message = $"Insufficient stock for {item.Medication?.Name}. Available: {item.Medication?.StockQuantity}, Required: {item.Quantity}"
                });
            }
        }

        // Deduct stock
        foreach (var item in prescription.PrescriptionItems)
        {
            if (item.Medication != null)
            {
                item.Medication.StockQuantity -= item.Quantity;
            }
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Prescription dispensed successfully" });
    }

    // Inventory Management
    [HttpGet]
    public async Task<IActionResult> Inventory(int page = 1, int pageSize = 20, string search = "", bool lowStockOnly = false)
    {
        var query = _context.Medications.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m => m.Name!.Contains(search) || m.Description!.Contains(search));
        }

        if (lowStockOnly)
        {
            query = query.Where(m => m.StockQuantity <= m.LowStockThreshold);
        }

        var totalCount = await query.CountAsync();

        var medications = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MedicationDto
            {
                MedicationId = m.MedicationId,
                Name = m.Name ?? "",
                Description = m.Description ?? "",
                UnitPrice = m.UnitPrice ?? 0,
                StockQuantity = m.StockQuantity ?? 0,
                LowStockThreshold = m.LowStockThreshold ?? 0,
                IsLowStock = (m.StockQuantity ?? 0) <= (m.LowStockThreshold ?? 0)
            })
            .ToListAsync();

        var viewModel = new MedicationListViewModel
        {
            Medications = medications,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search,
            ShowLowStockOnly = lowStockOnly
        };

        return View(viewModel);
    }

    // Add Medication
    [HttpGet]
    public IActionResult AddMedication()
    {
        return View(new CreateMedicationViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> AddMedication(CreateMedicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var medication = new Medication
        {
            Name = model.Name,
            Description = model.Description,
            UnitPrice = model.UnitPrice,
            StockQuantity = model.StockQuantity,
            LowStockThreshold = model.LowStockThreshold
        };

        _context.Medications.Add(medication);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Medication added successfully", medicationId = medication.MedicationId });
    }

    // Edit Medication
    [HttpGet]
    public async Task<IActionResult> EditMedication(int id)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null)
            return NotFound();

        var viewModel = new EditMedicationViewModel
        {
            MedicationId = medication.MedicationId,
            Name = medication.Name ?? "",
            Description = medication.Description ?? "",
            UnitPrice = medication.UnitPrice ?? 0,
            StockQuantity = medication.StockQuantity ?? 0,
            LowStockThreshold = medication.LowStockThreshold ?? 0
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EditMedication(EditMedicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var medication = await _context.Medications.FindAsync(model.MedicationId);
        if (medication == null)
            return Json(new { success = false, message = "Medication not found" });

        medication.Name = model.Name;
        medication.Description = model.Description;
        medication.UnitPrice = model.UnitPrice;
        medication.StockQuantity = model.StockQuantity;
        medication.LowStockThreshold = model.LowStockThreshold;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Medication updated successfully" });
    }

    // Update Stock
    [HttpGet]
    public async Task<IActionResult> UpdateStock(int id)
    {
        var medication = await _context.Medications.FindAsync(id);
        if (medication == null)
            return NotFound();

        var viewModel = new UpdateStockViewModel
        {
            MedicationId = medication.MedicationId,
            MedicationName = medication.Name ?? "",
            CurrentStock = medication.StockQuantity ?? 0
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(UpdateStockViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var medication = await _context.Medications.FindAsync(model.MedicationId);
        if (medication == null)
            return Json(new { success = false, message = "Medication not found" });

        medication.StockQuantity += model.QuantityToAdd;

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Stock updated successfully",
            newStock = medication.StockQuantity
        });
    }

    // Reports
    [HttpGet]
    public async Task<IActionResult> InventoryReport()
    {
        var medications = await _context.Medications
            .OrderBy(m => m.Name)
            .Select(m => new MedicationDto
            {
                MedicationId = m.MedicationId,
                Name = m.Name ?? "",
                Description = m.Description ?? "",
                UnitPrice = m.UnitPrice ?? 0,
                StockQuantity = m.StockQuantity ?? 0,
                LowStockThreshold = m.LowStockThreshold ?? 0,
                IsLowStock = (m.StockQuantity ?? 0) <= (m.LowStockThreshold ?? 0)
            })
            .ToListAsync();

        var viewModel = new PharmacyInventoryReportViewModel
        {
            ReportDate = DateTime.Now,
            TotalMedications = medications.Count,
            LowStockCount = medications.Count(m => m.IsLowStock),
            TotalInventoryValue = medications.Sum(m => m.UnitPrice * m.StockQuantity),
            AllMedications = medications,
            LowStockMedications = medications.Where(m => m.IsLowStock).ToList()
        };

        return View(viewModel);
    }

    // AJAX Endpoints
    [HttpGet]
    public async Task<IActionResult> GetPendingPrescriptions()
    {
        var prescriptions = await GetPendingPrescriptionsQueue();
        return Json(prescriptions);
    }

    [HttpGet]
    public async Task<IActionResult> GetMedicationStock(int medicationId)
    {
        var medication = await _context.Medications.FindAsync(medicationId);
        if (medication == null)
            return Json(new { success = false, message = "Medication not found" });

        return Json(new
        {
            success = true,
            medicationId = medication.MedicationId,
            name = medication.Name,
            stock = medication.StockQuantity,
            unitPrice = medication.UnitPrice,
            lowStockThreshold = medication.LowStockThreshold,
            isLowStock = (medication.StockQuantity ?? 0) <= (medication.LowStockThreshold ?? 0)
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchMedications(string term)
    {
        var medications = await _context.Medications
            .Where(m => m.Name!.Contains(term))
            .Take(10)
            .Select(m => new
            {
                id = m.MedicationId,
                text = m.Name,
                stock = m.StockQuantity,
                price = m.UnitPrice
            })
            .ToListAsync();

        return Json(medications);
    }

    [HttpGet]
    public async Task<IActionResult> LowStockAlerts()
    {
        var medications = await _context.Medications
            .Where(m => m.StockQuantity <= m.LowStockThreshold)
            .OrderBy(m => m.StockQuantity)
            .Select(m => new LowStockMedicationDto
            {
                MedicationId = m.MedicationId,
                Name = m.Name ?? "",
                StockQuantity = m.StockQuantity ?? 0,
                LowStockThreshold = m.LowStockThreshold ?? 0
            })
            .ToListAsync();

        var viewModel = new LowStockAlertViewModel
        {
            Alerts = medications,
            CriticalCount = medications.Count(m => m.StockQuantity < (m.LowStockThreshold * 0.1m)),
            WarningCount = medications.Count
        };

        return View(viewModel);
    }

    // Helper methods
    private async Task<int> GetPendingPrescriptionsCount()
    {
        // Count prescriptions that haven't been fully dispensed
        return await _context.Prescriptions
            .CountAsync(p => p.CreatedAt >= DateTime.Today.AddDays(-30)); // Last 30 days
    }

    private async Task<List<PendingPrescriptionDto>> GetPendingPrescriptionsQueue()
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .Where(p => p.CreatedAt >= DateTime.Today.AddDays(-7)) // Last 7 days
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        return prescriptions.Select(p =>
        {
            var items = p.PrescriptionItems.ToList();
            var canDispense = items.All(i => (i.Medication?.StockQuantity ?? 0) >= (i.Quantity ?? 0));

            return new PendingPrescriptionDto
            {
                PrescriptionId = p.PrescriptionId,
                PatientName = p.Visit?.Patient?.FullName ?? "",
                DoctorName = p.Doctor?.FullName ?? "",
                CreatedAt = p.CreatedAt ?? DateTime.MinValue,
                ItemCount = items.Count,
                TotalAmount = items.Sum(i => (i.Quantity ?? 0) * (i.Medication?.UnitPrice ?? 0)),
                Status = "Pending",
                CanDispense = canDispense,
                IssueReason = canDispense ? null : "Insufficient stock for some items"
            };
        }).ToList();
    }
}
