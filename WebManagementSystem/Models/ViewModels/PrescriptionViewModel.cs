namespace WebManagementSystem.Models.ViewModels;

public class PrescriptionListViewModel
{
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class PrescriptionDto
{
    public int PrescriptionId { get; set; }
    public int? VisitId { get; set; }
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreatePrescriptionViewModel
{
    public int VisitId { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }

    public List<PrescriptionItemViewModel> Items { get; set; } = new();

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public List<MedicationSelectDto> AvailableMedications { get; set; } = new();
}

public class PrescriptionItemViewModel
{
    public int? PrescriptionItemId { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
}

public class EditPrescriptionViewModel
{
    public int PrescriptionId { get; set; }
    public int VisitId { get; set; }
    public int DoctorId { get; set; }
    public int PatientId { get; set; }

    public List<PrescriptionItemViewModel> Items { get; set; } = new();

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PrescriptionDetailsViewModel
{
    public int PrescriptionId { get; set; }
    public int? VisitId { get; set; }

    // Patient Information
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;

    // Doctor Information
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Visit Information
    public DateTime? VisitDate { get; set; }
    public string? Diagnosis { get; set; }

    public List<PrescriptionItemDetailDto> Items { get; set; } = new();

    // Dispensing Information
    public string Status { get; set; } = string.Empty;
    public DateTime? DispensedAt { get; set; }
    public string? DispensedBy { get; set; }
}

public class PrescriptionItemDetailDto
{
    public int PrescriptionItemId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsDispensed { get; set; }
}

public class MedicationSelectDto
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
}

public class DispensePrescriptionViewModel
{
    public int PrescriptionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public List<DispenseItemDto> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
}

public class DispenseItemDto
{
    public int PrescriptionItemId { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool CanDispense { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
}
