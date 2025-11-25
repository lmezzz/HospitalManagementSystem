namespace WebManagementSystem.Models.ViewModels;

// Dashboard ViewModels for different roles

public class AdminDashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalAppointmentsToday { get; set; }
    public int TotalPendingBills { get; set; }
    public decimal TotalRevenueToday { get; set; }
    public decimal TotalRevenueMonth { get; set; }
    public int TotalPendingLabOrders { get; set; }
    public int LowStockMedicationsCount { get; set; }
    public List<RecentAppointmentDto> RecentAppointments { get; set; } = new();
    public List<TopDoctorDto> TopDoctors { get; set; } = new();
    public List<RevenueChartDto> RevenueChartData { get; set; } = new();
}

public class DoctorDashboardViewModel
{
    public string DoctorName { get; set; } = string.Empty;
    public int TodaysAppointments { get; set; }
    public int CompletedToday { get; set; }
    public int PendingToday { get; set; }
    public int TotalPatientsThisMonth { get; set; }
    public List<TodaysAppointmentDto> TodaysSchedule { get; set; } = new();
    public List<RecentVisitDto> RecentVisits { get; set; } = new();
}

public class PatientDashboardViewModel
{
    public string PatientName { get; set; } = string.Empty;
    public int? UpcomingAppointments { get; set; }
    public int? PendingLabResults { get; set; }
    public decimal? OutstandingBillAmount { get; set; }
    public List<UpcomingAppointmentDto> Appointments { get; set; } = new();
    public List<RecentPrescriptionDto> RecentPrescriptions { get; set; } = new();
    public List<PendingBillDto> PendingBills { get; set; } = new();
}

public class PharmacyDashboardViewModel
{
    public int PendingPrescriptions { get; set; }
    public int DispensedToday { get; set; }
    public int LowStockItems { get; set; }
    public decimal TotalSalesToday { get; set; }
    public List<PendingPrescriptionDto> PendingQueue { get; set; } = new();
    public List<LowStockMedicationDto> LowStockMedications { get; set; } = new();
}

public class LabDashboardViewModel
{
    public int PendingOrders { get; set; }
    public int CompletedToday { get; set; }
    public int InProgress { get; set; }
    public List<PendingLabOrderDto> PendingLabOrders { get; set; } = new();
    public List<RecentLabResultDto> RecentResults { get; set; } = new();
}

// Supporting DTOs
public class RecentAppointmentDto
{
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TopDoctorDto
{
    public string DoctorName { get; set; } = string.Empty;
    public int PatientCount { get; set; }
    public decimal Revenue { get; set; }
}

public class RevenueChartDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TodaysAppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RecentVisitDto
{
    public int VisitId { get; set; }
    public DateTime VisitTime { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
}

public class UpcomingAppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RecentPrescriptionDto
{
    public int PrescriptionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PendingBillDto
{
    public int BillId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingPrescriptionDto
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

public class LowStockMedicationDto
{
    public int MedicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
}

public class PendingLabOrderDto
{
    public int LabOrderId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class RecentLabResultDto
{
    public int LabResultId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
