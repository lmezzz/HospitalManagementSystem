using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly HmsContext _context;

    public BillingController(HmsContext context)
    {
        _context = context;
    }

    // List bills
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string filter = "All")
    {
        var query = _context.Bills
            .Include(b => b.Patient)
            .AsQueryable();

        // Apply filter
        if (filter == "Paid")
            query = query.Where(b => b.Status == "Paid");
        else if (filter == "Unpaid")
            query = query.Where(b => b.Status == "Unpaid");
        else if (filter == "Partial")
            query = query.Where(b => b.Status == "Partial");

        // Role-based filtering
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (userRole == "Patient")
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var patientId = await _context.Patients
                .Where(p => p.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefaultAsync();
            query = query.Where(b => b.PatientId == patientId);
        }

        var totalCount = await query.CountAsync();

        var bills = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var billDtos = new List<BillDto>();
        foreach (var bill in bills)
        {
            var totalPaid = await _context.Payments
                .Where(p => p.BillId == bill.BillId)
                .SumAsync(p => p.AmountPaid ?? 0);

            billDtos.Add(new BillDto
            {
                BillId = bill.BillId,
                PatientId = bill.PatientId,
                PatientName = bill.Patient?.FullName ?? "",
                TotalAmount = bill.TotalAmount ?? 0,
                AmountPaid = totalPaid,
                Balance = (bill.TotalAmount ?? 0) - totalPaid,
                Status = bill.Status ?? "",
                CreatedAt = bill.CreatedAt ?? DateTime.MinValue
            });
        }

        var viewModel = new BillListViewModel
        {
            Bills = billDtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            Filter = filter
        };

        return View(viewModel);
    }

    // Create bill
    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist,Billing")]
    public async Task<IActionResult> Create(int? patientId, int? visitId)
    {
        var viewModel = new CreateBillViewModel();

        if (patientId.HasValue)
        {
            viewModel.PatientId = patientId.Value;
            var patient = await _context.Patients.FindAsync(patientId.Value);
            viewModel.PatientName = patient?.FullName ?? "";
        }

        if (visitId.HasValue)
        {
            viewModel.VisitId = visitId.Value;

            // Auto-add visit consultation fee
            viewModel.Items.Add(new BillItemViewModel
            {
                ItemType = "Consultation",
                Description = "Doctor Consultation",
                Quantity = 1,
                UnitPrice = 1000, // Default consultation fee
                Amount = 1000
            });

            // Add prescribed medications
            var prescriptions = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medication)
                .Where(p => p.VisitId == visitId.Value)
                .ToListAsync();

            foreach (var prescription in prescriptions)
            {
                foreach (var item in prescription.PrescriptionItems)
                {
                    viewModel.Items.Add(new BillItemViewModel
                    {
                        ItemType = "Medication",
                        ReferenceId = item.MedicationId,
                        Description = item.Medication?.Name ?? "",
                        Quantity = item.Quantity ?? 1,
                        UnitPrice = item.Medication?.UnitPrice ?? 0,
                        Amount = (item.Quantity ?? 1) * (item.Medication?.UnitPrice ?? 0)
                    });
                }
            }

            // Add lab tests
            var labOrders = await _context.LabOrders
                .Include(l => l.LabTest)
                .Where(l => l.VisitId == visitId.Value)
                .ToListAsync();

            foreach (var labOrder in labOrders)
            {
                viewModel.Items.Add(new BillItemViewModel
                {
                    ItemType = "LabTest",
                    ReferenceId = labOrder.LabTestId,
                    Description = labOrder.LabTest?.TestName ?? "",
                    Quantity = 1,
                    UnitPrice = labOrder.LabTest?.Cost ?? 0,
                    Amount = labOrder.LabTest?.Cost ?? 0
                });
            }

            viewModel.SubTotal = viewModel.Items.Sum(i => i.Amount);
            viewModel.TotalAmount = viewModel.SubTotal;
        }

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist,Billing")]
    public async Task<IActionResult> Create(CreateBillViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var bill = new Bill
        {
            PatientId = model.PatientId,
            TotalAmount = model.TotalAmount,
            Status = "Unpaid",
            CreatedAt = DateTime.UtcNow
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Add bill items
        foreach (var item in model.Items)
        {
            var billItem = new BillItem
            {
                BillId = bill.BillId,
                ItemType = item.ItemType,
                ReferenceId = item.ReferenceId,
                Quantity = item.Quantity,
                Amount = item.Amount
            };
            _context.BillItems.Add(billItem);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Bill created successfully", billId = bill.BillId });
    }

    // Bill details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var bill = await _context.Bills
            .Include(b => b.Patient)
            .Include(b => b.BillItems)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);

        if (bill == null)
            return NotFound();

        var viewModel = new BillDetailsViewModel
        {
            BillId = bill.BillId,
            PatientId = bill.PatientId ?? 0,
            PatientName = bill.Patient?.FullName ?? "",
            PatientPhone = bill.Patient?.Phone ?? "",
            PatientAddress = bill.Patient?.Address ?? "",
            CreatedAt = bill.CreatedAt ?? DateTime.MinValue,
            Status = bill.Status ?? "",
            SubTotal = bill.BillItems.Sum(i => i.Amount ?? 0),
            TotalAmount = bill.TotalAmount ?? 0
        };

        viewModel.Items = bill.BillItems.Select(i => new BillItemDetailDto
        {
            BillItemId = i.BillItemId,
            ItemType = i.ItemType ?? "",
            Description = GetItemDescription(i.ItemType, i.ReferenceId),
            Quantity = i.Quantity ?? 1,
            UnitPrice = (i.Amount ?? 0) / (i.Quantity ?? 1),
            Amount = i.Amount ?? 0
        }).ToList();

        viewModel.Payments = bill.Payments.Select(p => new PaymentDetailDto
        {
            PaymentId = p.PaymentId,
            AmountPaid = p.AmountPaid ?? 0,
            PaymentMethod = p.PaymentMethod ?? "",
            PaymentTime = p.PaymentTime ?? DateTime.MinValue
        }).ToList();

        viewModel.TotalPaid = viewModel.Payments.Sum(p => p.AmountPaid);
        viewModel.Balance = viewModel.TotalAmount - viewModel.TotalPaid;

        return View(viewModel);
    }

    // Process payment
    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist,Billing")]
    public async Task<IActionResult> ProcessPayment(int billId)
    {
        var bill = await _context.Bills
            .Include(b => b.Patient)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == billId);

        if (bill == null)
            return NotFound();

        var totalPaid = bill.Payments.Sum(p => p.AmountPaid ?? 0);

        var viewModel = new CreatePaymentViewModel
        {
            BillId = billId,
            PatientName = bill.Patient?.FullName ?? "",
            TotalAmount = bill.TotalAmount ?? 0,
            PreviouslyPaid = totalPaid,
            Balance = (bill.TotalAmount ?? 0) - totalPaid,
            AmountPaid = (bill.TotalAmount ?? 0) - totalPaid // Default to full balance
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist,Billing")]
    public async Task<IActionResult> ProcessPayment(CreatePaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var bill = await _context.Bills
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == model.BillId);

        if (bill == null)
            return Json(new { success = false, message = "Bill not found" });

        var payment = new Payment
        {
            BillId = model.BillId,
            AmountPaid = model.AmountPaid,
            PaymentMethod = model.PaymentMethod,
            PaymentTime = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        // Update bill status
        var totalPaid = bill.Payments.Sum(p => p.AmountPaid ?? 0) + model.AmountPaid;
        var totalAmount = bill.TotalAmount ?? 0;

        if (totalPaid >= totalAmount)
            bill.Status = "Paid";
        else if (totalPaid > 0)
            bill.Status = "Partial";
        else
            bill.Status = "Unpaid";

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Payment processed successfully", paymentId = payment.PaymentId });
    }

    // Print invoice
    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var bill = await _context.Bills
            .Include(b => b.Patient)
            .Include(b => b.BillItems)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);

        if (bill == null)
            return NotFound();

        var viewModel = new InvoiceViewModel
        {
            BillId = bill.BillId,
            InvoiceDate = bill.CreatedAt ?? DateTime.MinValue,
            InvoiceNumber = $"INV-{bill.BillId:D6}",
            PatientName = bill.Patient?.FullName ?? "",
            PatientPhone = bill.Patient?.Phone ?? "",
            PatientAddress = bill.Patient?.Address ?? "",
            SubTotal = bill.BillItems.Sum(i => i.Amount ?? 0),
            TotalAmount = bill.TotalAmount ?? 0,
            TotalPaid = bill.Payments.Sum(p => p.AmountPaid ?? 0),
            Status = bill.Status ?? ""
        };

        viewModel.Items = bill.BillItems.Select(i => new BillItemDetailDto
        {
            BillItemId = i.BillItemId,
            ItemType = i.ItemType ?? "",
            Description = GetItemDescription(i.ItemType, i.ReferenceId),
            Quantity = i.Quantity ?? 1,
            UnitPrice = (i.Amount ?? 0) / (i.Quantity ?? 1),
            Amount = i.Amount ?? 0
        }).ToList();

        viewModel.Balance = viewModel.TotalAmount - viewModel.TotalPaid;

        return View(viewModel);
    }

    // Payment receipt
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Bill)
            .ThenInclude(b => b!.Patient)
            .Include(p => p.Bill)
            .ThenInclude(b => b!.Payments)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
            return NotFound();

        var viewModel = new PaymentReceiptViewModel
        {
            PaymentId = payment.PaymentId,
            BillId = payment.BillId ?? 0,
            PatientName = payment.Bill?.Patient?.FullName ?? "",
            PatientPhone = payment.Bill?.Patient?.Phone ?? "",
            AmountPaid = payment.AmountPaid ?? 0,
            PaymentMethod = payment.PaymentMethod ?? "",
            PaymentTime = payment.PaymentTime ?? DateTime.MinValue,
            ReferenceNumber = $"REC-{payment.PaymentId:D6}",
            TotalBillAmount = payment.Bill?.TotalAmount ?? 0,
            TotalPaid = payment.Bill?.Payments.Sum(p => p.AmountPaid ?? 0) ?? 0
        };

        viewModel.RemainingBalance = viewModel.TotalBillAmount - viewModel.TotalPaid;

        return View(viewModel);
    }

    // AJAX: Get bill summary
    [HttpGet]
    public async Task<IActionResult> GetBillSummary(int billId)
    {
        var bill = await _context.Bills
            .Include(b => b.BillItems)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == billId);

        if (bill == null)
            return Json(new { success = false, message = "Bill not found" });

        var totalPaid = bill.Payments.Sum(p => p.AmountPaid ?? 0);

        return Json(new
        {
            success = true,
            billId = bill.BillId,
            totalAmount = bill.TotalAmount,
            totalPaid,
            balance = (bill.TotalAmount ?? 0) - totalPaid,
            status = bill.Status,
            itemCount = bill.BillItems.Count
        });
    }

    // AJAX: Search bills
    [HttpGet]
    public async Task<IActionResult> SearchBills(string term, int page = 1, int pageSize = 10)
    {
        var query = _context.Bills
            .Include(b => b.Patient)
            .Where(b => b.Patient!.FullName!.Contains(term) ||
                       b.Patient.Phone!.Contains(term) ||
                       b.Patient.Cnic!.Contains(term));

        var totalCount = await query.CountAsync();

        var bills = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.BillId,
                PatientName = b.Patient!.FullName,
                b.TotalAmount,
                TotalPaid = _context.Payments.Where(p => p.BillId == b.BillId).Sum(p => p.AmountPaid),
                b.Status,
                CreatedAt = b.CreatedAt!.Value.ToString("MMM dd, yyyy")
            })
            .ToListAsync();

        return Json(new { data = bills, totalCount, page, pageSize });
    }

    // Helper method
    private string GetItemDescription(string? itemType, int? referenceId)
    {
        if (string.IsNullOrEmpty(itemType) || !referenceId.HasValue)
            return itemType ?? "Unknown";

        return itemType switch
        {
            "Medication" => _context.Medications.Find(referenceId.Value)?.Name ?? "Medication",
            "LabTest" => _context.LabTests.Find(referenceId.Value)?.TestName ?? "Lab Test",
            "Consultation" => "Doctor Consultation",
            _ => itemType
        };
    }

    // Reports
    [HttpGet]
    [Authorize(Roles = "Admin,Billing")]
    public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today.AddMonths(-1);
        var end = endDate ?? DateTime.Today;

        var bills = await _context.Bills
            .Include(b => b.Patient)
            .Include(b => b.Payments)
            .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
            .ToListAsync();

        var billDtos = bills.Select(b => new BillDto
        {
            BillId = b.BillId,
            PatientName = b.Patient?.FullName ?? "",
            TotalAmount = b.TotalAmount ?? 0,
            AmountPaid = b.Payments.Sum(p => p.AmountPaid ?? 0),
            Balance = (b.TotalAmount ?? 0) - b.Payments.Sum(p => p.AmountPaid ?? 0),
            Status = b.Status ?? "",
            CreatedAt = b.CreatedAt ?? DateTime.MinValue
        }).ToList();

        var viewModel = new BillSummaryReportViewModel
        {
            StartDate = start,
            EndDate = end,
            TotalBills = bills.Count,
            TotalAmount = bills.Sum(b => b.TotalAmount ?? 0),
            TotalPaid = bills.SelectMany(b => b.Payments).Sum(p => p.AmountPaid ?? 0),
            TotalOutstanding = billDtos.Sum(b => b.Balance),
            Bills = billDtos
        };

        return View(viewModel);
    }
}
