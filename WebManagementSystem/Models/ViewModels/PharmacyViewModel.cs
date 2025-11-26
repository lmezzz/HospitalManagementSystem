namespace WebManagementSystem.Models.ViewModels;

public class MedicationListViewModel
{
    public List<MedicationDto> Medications { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowLowStockOnly { get; set; }
}

public class MedicationDto
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock { get; set; }
}

public class CreateMedicationViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public string? Manufacturer { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
}

public class EditMedicationViewModel
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public string? Manufacturer { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
}

public class MedicationDetailsViewModel
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock { get; set; }
    public string? Manufacturer { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }

    // Usage Statistics
    public int TotalDispensed { get; set; }
    public int TimesOrdered { get; set; }
    public DateTime? LastDispensedDate { get; set; }
}

public class UpdateStockViewModel
{
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int QuantityToAdd { get; set; }
    public int NewStock => CurrentStock + QuantityToAdd;
    public string? Notes { get; set; }
    public string TransactionType { get; set; } = "Purchase"; // Purchase, Return, Adjustment
}

public class StockTransactionViewModel
{
    public int TransactionId { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public string? PerformedBy { get; set; }
}

public class PharmacyInventoryReportViewModel
{
    public DateTime ReportDate { get; set; }
    public int TotalMedications { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<MedicationDto> AllMedications { get; set; } = new();
    public List<MedicationDto> LowStockMedications { get; set; } = new();
}

public class PrescriptionQueueViewModel
{
    public List<PrescriptionQueueItemDto> Queue { get; set; } = new();
    public int PendingCount { get; set; }
    public int DispensedTodayCount { get; set; }
}

public class PrescriptionQueueItemDto
{
    public int PrescriptionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool CanDispense { get; set; }
    public string? IssueReason { get; set; }
}

public class DispenseMedicationViewModel
{
    public int PrescriptionId { get; set; }
    public int PrescriptionItemId { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

public class PharmacySalesReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalPrescriptionsDispensed { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<MedicationSalesDto> TopSellingMedications { get; set; } = new();
    public List<DailySalesDto> DailySales { get; set; } = new();
}

public class MedicationSalesDto
{
    public string MedicationName { get; set; } = string.Empty;
    public int QuantityDispensed { get; set; }
    public decimal Revenue { get; set; }
}

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public int PrescriptionsCount { get; set; }
    public decimal Revenue { get; set; }
}

public class LowStockAlertViewModel
{
    public List<LowStockMedicationDto> Alerts { get; set; } = new();
    public int CriticalCount { get; set; } // Stock < 10% of threshold
    public int WarningCount { get; set; } // Stock below threshold
}
