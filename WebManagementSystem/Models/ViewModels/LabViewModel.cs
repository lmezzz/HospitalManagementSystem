namespace WebManagementSystem.Models.ViewModels;

public class LabOrderListViewModel
{
    public List<LabOrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string StatusFilter { get; set; } = "All"; // All, Pending, InProgress, Completed
}

public class LabOrderDto
{
    public int LabOrderId { get; set; }
    public int? VisitId { get; set; }
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public DateTime? SampleTime { get; set; }
    public DateTime? CompletedTime { get; set; }
}

public class CreateLabOrderViewModel
{
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public List<int> LabTestIds { get; set; } = new();
    public string Priority { get; set; } = "Normal"; // Stat, Urgent, Normal, Routine
    public string? ClinicalNotes { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public List<LabTestSelectDto> AvailableTests { get; set; } = new();
}

public class EditLabOrderViewModel
{
    public int LabOrderId { get; set; }
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int LabTestId { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public DateTime? SampleTime { get; set; }
    public DateTime? CompletedTime { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
}

public class LabOrderDetailsViewModel
{
    public int LabOrderId { get; set; }
    public int? VisitId { get; set; }

    // Patient Information
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;

    // Doctor Information
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    // Test Information
    public int LabTestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string TestCategory { get; set; } = string.Empty;
    public string TestDescription { get; set; } = string.Empty;
    public decimal TestCost { get; set; }

    // Order Details
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public DateTime? SampleTime { get; set; }
    public DateTime? CompletedTime { get; set; }

    // Results
    public List<LabResultDto> Results { get; set; } = new();

    // Clinical Information
    public string? Diagnosis { get; set; }
    public string? ClinicalNotes { get; set; }
}

public class LabResultDto
{
    public int LabResultId { get; set; }
    public int LabOrderId { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? UploadedBy { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class UploadLabResultViewModel
{
    public int LabOrderId { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public IFormFile? ResultFile { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
}

public class LabTestSelectDto
{
    public int LabTestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class LabTestListViewModel
{
    public List<LabTestDto> Tests { get; set; } = new();
    public int TotalCount { get; set; }
}

public class LabTestDto
{
    public int LabTestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class DoctorLabRequestViewModel
{
    public CreateLabOrderViewModel? Form { get; set; }
    public List<VisitDto> RecentVisits { get; set; } = new();
    public bool HasVisit => Form != null;
}

public class CreateLabTestViewModel
{
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class EditLabTestViewModel
{
    public int LabTestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PatientLabHistoryViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<LabOrderDto> Orders { get; set; } = new();
}

public class LabWorkloadReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal AverageTurnaroundHours { get; set; }
    public List<TestCategoryStatsDto> CategoryStats { get; set; } = new();
}

public class TestCategoryStatsDto
{
    public string Category { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class LabResultsListViewModel
{
    public List<LabResultDto> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
