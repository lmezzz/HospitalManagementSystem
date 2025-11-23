namespace WebManagementSystem.Models.ViewModels;

public class VisitListViewModel
{
    public List<VisitDto> Visits { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class VisitDto
{
    public int VisitId { get; set; }
    public int? AppointmentId { get; set; }
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime? VisitTime { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateVisitViewModel
{
    public int? AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime VisitTime { get; set; }

    // Vital Signs
    public decimal? Temperature { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }

    // Clinical Information
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class EditVisitViewModel
{
    public int VisitId { get; set; }
    public int? AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime VisitTime { get; set; }

    // Vital Signs
    public decimal? Temperature { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }

    // Clinical Information
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class VisitDetailsViewModel
{
    public int VisitId { get; set; }
    public int? AppointmentId { get; set; }

    // Patient Information
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public string PatientGender { get; set; } = string.Empty;

    // Doctor Information
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;

    public DateTime VisitTime { get; set; }

    // Vital Signs
    public decimal? Temperature { get; set; }
    public string? BloodPressure { get; set; }
    public int? HeartRate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? BMI { get; set; }

    // Clinical Information
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;

    // Related Data
    public List<PrescriptionSummaryDto> Prescriptions { get; set; } = new();
    public List<LabOrderSummaryDto> LabOrders { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}

public class PrescriptionSummaryDto
{
    public int PrescriptionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LabOrderSummaryDto
{
    public int LabOrderId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
}

public class PatientMedicalHistoryViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }

    public List<VisitHistoryDto> VisitHistory { get; set; } = new();
    public List<PrescriptionHistoryDto> PrescriptionHistory { get; set; } = new();
    public List<LabHistoryDto> LabHistory { get; set; } = new();
}

public class VisitHistoryDto
{
    public int VisitId { get; set; }
    public DateTime VisitTime { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
}

public class PrescriptionHistoryDto
{
    public int PrescriptionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public List<string> Medications { get; set; } = new();
}

public class LabHistoryDto
{
    public int LabOrderId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedTime { get; set; }
}
