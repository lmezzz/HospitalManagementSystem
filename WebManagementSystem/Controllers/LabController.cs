using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebManagementSystem.Models;
using WebManagementSystem.Models.ViewModels;

namespace WebManagementSystem.Controllers;

[Authorize]
public class LabController : Controller
{
    private readonly HmsContext _context;
    private readonly IWebHostEnvironment _environment;

    public LabController(HmsContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // Dashboard
    [Authorize(Roles = "Admin,LabTechnician")]
    public async Task<IActionResult> Dashboard()
    {
        var viewModel = new LabDashboardViewModel
        {
            PendingOrders = await _context.LabOrders.CountAsync(l => l.Status == "Pending"),
            CompletedToday = await _context.LabOrders
                .CountAsync(l => l.CompletedTime.HasValue && l.CompletedTime.Value.Date == DateTime.Today),
            InProgress = await _context.LabOrders.CountAsync(l => l.Status == "InProgress")
        };

        viewModel.PendingLabOrders = await _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.LabTest)
            .Where(l => l.Status == "Pending" || l.Status == "InProgress")
            .OrderBy(l => l.OrderTime)
            .Take(20)
            .Select(l => new PendingLabOrderDto
            {
                LabOrderId = l.LabOrderId,
                PatientName = l.Patient!.FullName ?? "",
                TestName = l.LabTest!.TestName ?? "",
                OrderTime = l.OrderTime ?? DateTime.MinValue,
                Priority = l.Priority ?? "",
                Status = l.Status ?? ""
            })
            .ToListAsync();

        viewModel.RecentResults = await _context.LabResults
            .Include(r => r.LabOrder)
            .ThenInclude(o => o!.Patient)
            .OrderByDescending(r => r.UploadedAt)
            .Take(10)
            .Select(r => new RecentLabResultDto
            {
                LabResultId = r.LabResultId,
                PatientName = r.LabOrder!.Patient!.FullName ?? "",
                TestName = r.LabOrder.LabTest!.TestName ?? "",
                UploadedAt = r.UploadedAt ?? DateTime.MinValue
            })
            .ToListAsync();

        return View(viewModel);
    }

    // List lab orders
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string statusFilter = "All")
    {
        var query = _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.Doctor)
            .Include(l => l.LabTest)
            .AsQueryable();

        if (statusFilter != "All")
            query = query.Where(l => l.Status == statusFilter);

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(l => l.OrderTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LabOrderDto
            {
                LabOrderId = l.LabOrderId,
                VisitId = l.VisitId,
                PatientId = l.PatientId,
                PatientName = l.Patient!.FullName ?? "",
                DoctorId = l.DoctorId,
                DoctorName = l.Doctor!.FullName ?? "",
                TestName = l.LabTest!.TestName ?? "",
                Priority = l.Priority ?? "",
                Status = l.Status ?? "",
                OrderTime = l.OrderTime ?? DateTime.MinValue,
                SampleTime = l.SampleTime,
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        var viewModel = new LabOrderListViewModel
        {
            Orders = orders,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            StatusFilter = statusFilter
        };

        return View(viewModel);
    }

    // Create lab order
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(int? visitId)
    {
        var viewModel = new CreateLabOrderViewModel();

        if (visitId.HasValue)
        {
            var visit = await _context.Visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync(v => v.VisitId == visitId.Value);

            if (visit != null)
            {
                viewModel.VisitId = visit.VisitId;
                viewModel.PatientId = visit.PatientId ?? 0;
                viewModel.DoctorId = visit.DoctorId ?? 0;
                viewModel.PatientName = visit.Patient?.FullName ?? "";
                viewModel.DoctorName = visit.Doctor?.FullName ?? "";
            }
        }

        viewModel.AvailableTests = await _context.LabTests
            .Select(t => new LabTestSelectDto
            {
                LabTestId = t.LabTestId,
                TestName = t.TestName ?? "",
                Category = t.Category ?? "",
                Cost = t.Cost ?? 0,
                Description = t.Description ?? ""
            })
            .ToListAsync();

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(CreateLabOrderViewModel model)
    {
        if (!ModelState.IsValid || !model.LabTestIds.Any())
        {
            return Json(new { success = false, message = "Please select at least one test" });
        }

        var orderIds = new List<int>();

        foreach (var testId in model.LabTestIds)
        {
            var order = new LabOrder
            {
                VisitId = model.VisitId,
                PatientId = model.PatientId,
                DoctorId = model.DoctorId,
                LabTestId = testId,
                Priority = model.Priority,
                Status = "Pending",
                OrderTime = DateTime.UtcNow
            };

            _context.LabOrders.Add(order);
            await _context.SaveChangesAsync();
            orderIds.Add(order.LabOrderId);
        }

        return Json(new { success = true, message = "Lab orders created successfully", orderIds });
    }

    // Lab order details
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.Doctor)
            .Include(l => l.LabTest)
            .Include(l => l.Visit)
            .Include(l => l.LabResults)
            .ThenInclude(r => r.UploadedByNavigation)
            .FirstOrDefaultAsync(l => l.LabOrderId == id);

        if (order == null)
            return NotFound();

        var viewModel = new LabOrderDetailsViewModel
        {
            LabOrderId = order.LabOrderId,
            VisitId = order.VisitId,
            PatientId = order.PatientId ?? 0,
            PatientName = order.Patient?.FullName ?? "",
            PatientAge = order.Patient?.DateOfBirth.HasValue == true
                ? DateTime.Today.Year - order.Patient.DateOfBirth.Value.Year
                : null,
            PatientGender = order.Patient?.Gender ?? "",
            PatientPhone = order.Patient?.Phone ?? "",
            DoctorId = order.DoctorId ?? 0,
            DoctorName = order.Doctor?.FullName ?? "",
            LabTestId = order.LabTestId ?? 0,
            TestName = order.LabTest?.TestName ?? "",
            TestCategory = order.LabTest?.Category ?? "",
            TestDescription = order.LabTest?.Description ?? "",
            TestCost = order.LabTest?.Cost ?? 0,
            Priority = order.Priority ?? "",
            Status = order.Status ?? "",
            OrderTime = order.OrderTime ?? DateTime.MinValue,
            SampleTime = order.SampleTime,
            CompletedTime = order.CompletedTime,
            Diagnosis = order.Visit?.Diagnosis
        };

        viewModel.Results = order.LabResults.Select(r => new LabResultDto
        {
            LabResultId = r.LabResultId,
            LabOrderId = r.LabOrderId ?? 0,
            ResultText = r.ResultText ?? "",
            FilePath = r.FilePath,
            UploadedBy = r.UploadedBy,
            UploadedByName = r.UploadedByNavigation?.FullName ?? "",
            UploadedAt = r.UploadedAt ?? DateTime.MinValue
        }).ToList();

        return View(viewModel);
    }

    // Update order status
    [HttpPost]
    [Authorize(Roles = "Admin,LabTechnician")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _context.LabOrders.FindAsync(id);
        if (order == null)
            return Json(new { success = false, message = "Order not found" });

        order.Status = status;

        if (status == "InProgress" && !order.SampleTime.HasValue)
            order.SampleTime = DateTime.UtcNow;
        else if (status == "Completed" && !order.CompletedTime.HasValue)
            order.CompletedTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Status updated successfully" });
    }

    // Upload result
    [HttpGet]
    [Authorize(Roles = "Admin,LabTechnician")]
    public async Task<IActionResult> UploadResult(int orderId)
    {
        var order = await _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.LabTest)
            .FirstOrDefaultAsync(l => l.LabOrderId == orderId);

        if (order == null)
            return NotFound();

        var viewModel = new UploadLabResultViewModel
        {
            LabOrderId = order.LabOrderId,
            PatientName = order.Patient?.FullName ?? "",
            TestName = order.LabTest?.TestName ?? "",
            OrderTime = order.OrderTime ?? DateTime.MinValue
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,LabTechnician")]
    public async Task<IActionResult> UploadResult(UploadLabResultViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        string? filePath = null;

        // Handle file upload if provided
        if (model.ResultFile != null && model.ResultFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lab-results");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{model.LabOrderId}_{Guid.NewGuid()}{Path.GetExtension(model.ResultFile.FileName)}";
            filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ResultFile.CopyToAsync(fileStream);
            }

            filePath = $"/uploads/lab-results/{uniqueFileName}";
        }

        var result = new LabResult
        {
            LabOrderId = model.LabOrderId,
            ResultText = model.ResultText,
            FilePath = filePath,
            UploadedBy = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.LabResults.Add(result);

        // Update order status to completed
        var order = await _context.LabOrders.FindAsync(model.LabOrderId);
        if (order != null)
        {
            order.Status = "Completed";
            order.CompletedTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Result uploaded successfully", resultId = result.LabResultId });
    }

    // Lab tests management
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Tests()
    {
        var tests = await _context.LabTests
            .Select(t => new LabTestDto
            {
                LabTestId = t.LabTestId,
                TestName = t.TestName ?? "",
                Category = t.Category ?? "",
                Cost = t.Cost ?? 0,
                Description = t.Description ?? ""
            })
            .ToListAsync();

        var viewModel = new LabTestListViewModel
        {
            Tests = tests,
            TotalCount = tests.Count
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTest(CreateLabTestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var test = new LabTest
        {
            TestName = model.TestName,
            Category = model.Category,
            Cost = model.Cost,
            Description = model.Description
        };

        _context.LabTests.Add(test);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Test created successfully", testId = test.LabTestId });
    }

    // Patient lab history
    [HttpGet]
    public async Task<IActionResult> PatientHistory(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null)
            return NotFound();

        var orders = await _context.LabOrders
            .Include(l => l.Doctor)
            .Include(l => l.LabTest)
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.OrderTime)
            .Select(l => new LabOrderDto
            {
                LabOrderId = l.LabOrderId,
                TestName = l.LabTest!.TestName ?? "",
                DoctorName = l.Doctor!.FullName ?? "",
                OrderTime = l.OrderTime ?? DateTime.MinValue,
                Status = l.Status ?? "",
                CompletedTime = l.CompletedTime
            })
            .ToListAsync();

        var viewModel = new PatientLabHistoryViewModel
        {
            PatientId = patientId,
            PatientName = patient.FullName ?? "",
            Orders = orders
        };

        return View(viewModel);
    }

    // AJAX: Get pending orders
    [HttpGet]
    public async Task<IActionResult> GetPendingOrders()
    {
        var orders = await _context.LabOrders
            .Include(l => l.Patient)
            .Include(l => l.LabTest)
            .Where(l => l.Status == "Pending" || l.Status == "InProgress")
            .OrderBy(l => l.Priority == "Stat" ? 1 : l.Priority == "Urgent" ? 2 : 3)
            .ThenBy(l => l.OrderTime)
            .Select(l => new
            {
                l.LabOrderId,
                PatientName = l.Patient!.FullName,
                TestName = l.LabTest!.TestName,
                l.Priority,
                l.Status,
                OrderTime = l.OrderTime!.Value.ToString("MMM dd, yyyy HH:mm")
            })
            .ToListAsync();

        return Json(orders);
    }
}
