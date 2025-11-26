namespace WebManagementSystem.Models.ViewModels;

public class PatientListViewModel
{
    public List<PatientDto> Patients { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
}

public class PatientDto
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? LastVisit { get; set; }
}

public class PatientDetailsViewModel
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? EmergencyContact { get; set; }
    public DateTime RegisteredDate { get; set; }

    // Statistics
    public int TotalVisits { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime? LastVisit { get; set; }

    // Recent Activity
    public List<RecentAppointmentDto> RecentAppointments { get; set; } = new();
    public List<RecentVisitDto> RecentVisits { get; set; } = new();
    public List<RecentPrescriptionDto> RecentPrescriptions { get; set; } = new();
}

public class EditPatientViewModel
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? EmergencyContact { get; set; }
}

public class PatientSearchViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string SearchType { get; set; } = "Name"; // Name, CNIC, Phone
    public List<PatientSearchResultDto> Results { get; set; } = new();
}

public class PatientSearchResultDto
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public DateTime? LastVisit { get; set; }
}

public class PatientAppointmentHistoryViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public List<AppointmentHistoryDto> Appointments { get; set; } = new();
}

public class AppointmentHistoryDto
{
    public int AppointmentId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasVisit { get; set; }
    public int? VisitId { get; set; }
}

public class PatientBillingHistoryViewModel
{
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public List<BillHistoryDto> Bills { get; set; } = new();
}

public class BillHistoryDto
{
    public int BillId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}
