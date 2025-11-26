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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DispenseMedication(int prescriptionId)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

        if (prescription == null)
        {
            TempData["ErrorMessage"] = "Prescription not found";
            return RedirectToAction("IssueMedicine");
        }

        // Check if already dispensed (has a bill)
        var existingBill = await _context.BillItems
            .Where(bi => bi.ItemType == "Prescription" && bi.ReferenceId == prescriptionId)
            .Select(bi => bi.Bill)
            .FirstOrDefaultAsync();

        if (existingBill != null)
        {
            TempData["ErrorMessage"] = $"Prescription #{prescriptionId} has already been dispensed. Bill ID: {existingBill.BillId}";
            return RedirectToAction("IssueMedicine", new { prescriptionId = prescriptionId });
        }

        // Check stock availability for all items
        foreach (var item in prescription.PrescriptionItems)
        {
            if ((item.Medication?.StockQuantity ?? 0) < (item.Quantity ?? 0))
            {
                TempData["ErrorMessage"] = $"Insufficient stock for {item.Medication?.Name}. Available: {item.Medication?.StockQuantity}, Required: {item.Quantity}";
                return RedirectToAction("IssueMedicine", new { prescriptionId = prescriptionId });
            }
        }

        // Calculate total amount
        var totalAmount = prescription.PrescriptionItems.Sum(i => 
            (i.Quantity ?? 0) * (i.Medication?.UnitPrice ?? 0));

        // Create bill for the prescription
        var bill = new Bill
        {
            PatientId = prescription.Visit?.PatientId,
            TotalAmount = totalAmount,
            Status = "Unpaid",
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync(); // Save to get BillId

        // Add bill items for each medication
        foreach (var item in prescription.PrescriptionItems)
        {
            var billItem = new BillItem
            {
                BillId = bill.BillId,
                ItemType = "Prescription",
                ReferenceId = prescriptionId, // Link to prescription
                Quantity = item.Quantity,
                Amount = (item.Quantity ?? 0) * (item.Medication?.UnitPrice ?? 0)
            };
            _context.BillItems.Add(billItem);
            }

        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Prescription #{prescriptionId} dispensed successfully! Bill #{bill.BillId} created for Rs. {totalAmount:N2}. Stock will be deducted from inventory when payment is received. <a href='/Billing/Details/{bill.BillId}' style='color: #28a745; text-decoration: underline;'>View Bill</a>";
            TempData["InfoMessage"] = $"Bill #{bill.BillId} created. Patient: {prescription.Visit?.Patient?.FullName}. Total: Rs. {totalAmount:N2}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error creating bill in database: {ex.Message}";
            return RedirectToAction("IssueMedicine", new { prescriptionId = prescriptionId });
        }

        return RedirectToAction("IssueMedicine");
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

        return View("Stock", viewModel);
    }

    // Alias for Inventory (Stock view)
    [HttpGet]
    public async Task<IActionResult> Stock(int page = 1, int pageSize = 20, string search = "", bool lowStockOnly = false)
    {
        return await Inventory(page, pageSize, search, lowStockOnly);
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
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return View(model);
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
        
        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Medication '{model.Name}' added successfully! Medication ID: {medication.MedicationId}. Initial stock: {model.StockQuantity} units. Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error adding medication to database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Inventory");
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
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return View(model);
        }

        var medication = await _context.Medications.FindAsync(model.MedicationId);
        if (medication == null)
        {
            TempData["ErrorMessage"] = "Medication not found";
            return RedirectToAction("Inventory");
        }

        medication.Name = model.Name;
        medication.Description = model.Description;
        medication.UnitPrice = model.UnitPrice;
        medication.StockQuantity = model.StockQuantity;
        medication.LowStockThreshold = model.LowStockThreshold;

        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Medication '{model.Name}' (ID: {model.MedicationId}) updated successfully! Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error updating medication in database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Inventory");
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
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["ErrorMessage"] = $"Validation failed: {errors}";
            return View(model);
        }

        var medication = await _context.Medications.FindAsync(model.MedicationId);
        if (medication == null)
        {
            TempData["ErrorMessage"] = "Medication not found";
            return RedirectToAction("Inventory");
        }

        var oldStock = medication.StockQuantity ?? 0;
        medication.StockQuantity = oldStock + model.QuantityToAdd;

        try
        {
        await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Stock updated successfully! {model.MedicationName}: {oldStock} → {medication.StockQuantity} units (+{model.QuantityToAdd}). Database updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error updating stock in database: {ex.Message}";
            return View(model);
        }

        return RedirectToAction("Inventory");
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
        // Get all bills that are linked to prescriptions
        var dispensedPrescriptionIds = await _context.BillItems
            .Where(bi => bi.ItemType == "Prescription")
            .Select(bi => bi.ReferenceId ?? 0)
            .Distinct()
            .ToListAsync();

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Visit)
            .ThenInclude(v => v!.Patient)
            .Include(p => p.PrescriptionItems)
            .ThenInclude(i => i.Medication)
            .Where(p => p.CreatedAt >= DateTime.Today.AddDays(-30) && // Last 30 days
                       !dispensedPrescriptionIds.Contains(p.PrescriptionId)) // Not yet dispensed
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
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
