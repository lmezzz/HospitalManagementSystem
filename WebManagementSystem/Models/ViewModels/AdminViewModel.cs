namespace WebManagementSystem.Models.ViewModels;

public class UserListViewModel
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int? RoleFilter { get; set; }
    public bool? ActiveFilter { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    // Helper properties
    public List<RoleSelectDto> AvailableRoles { get; set; } = new();
}

public class EditUserViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; }

    // Optional password change
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }

    // Helper properties
    public string CurrentRole { get; set; } = string.Empty;
    public List<RoleSelectDto> AvailableRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class UserDetailsViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Role-specific details
    public DoctorStatsDto? DoctorStats { get; set; }
    public PatientStatsDto? PatientStats { get; set; }
}

public class RoleSelectDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class DoctorStatsDto
{
    public int TotalAppointments { get; set; }
    public int TotalPatients { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? LastAppointment { get; set; }
}

public class PatientStatsDto
{
    public int TotalAppointments { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public DateTime? LastVisit { get; set; }
}

public class RoleListViewModel
{
    public List<RoleDto> Roles { get; set; } = new();
}

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public class SystemReportViewModel
{
    public DateTime ReportDate { get; set; }

    // User Statistics
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }

    // Appointment Statistics
    public int TotalAppointments { get; set; }
    public int AppointmentsToday { get; set; }
    public int AppointmentsThisWeek { get; set; }
    public int AppointmentsThisMonth { get; set; }

    // Financial Statistics
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisWeek { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal TotalOutstanding { get; set; }

    // Clinical Statistics
    public int TotalVisits { get; set; }
    public int VisitsToday { get; set; }
    public int PendingLabOrders { get; set; }
    public int PendingPrescriptions { get; set; }

    // Inventory Statistics
    public int TotalMedications { get; set; }
    public int LowStockMedications { get; set; }

    // Charts Data
    public List<AppointmentTrendDto> AppointmentTrends { get; set; } = new();
    public List<RevenueTrendDto> RevenueTrends { get; set; } = new();
}

public class AppointmentTrendDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class RevenueTrendDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class DoctorPerformanceReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DoctorPerformanceDto> Doctors { get; set; } = new();
}

public class DoctorPerformanceDto
{
    public string DoctorName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedVisits { get; set; }
    public int CancelledAppointments { get; set; }
    public int TotalPatients { get; set; }
    public decimal RevenueGenerated { get; set; }
    public double AverageRating { get; set; }
}

public class PatientDemographicsReportViewModel
{
    public int TotalPatients { get; set; }
    public List<GenderDistributionDto> GenderDistribution { get; set; } = new();
    public List<AgeGroupDistributionDto> AgeDistribution { get; set; } = new();
    public Dictionary<string, int> TopDiagnoses { get; set; } = new();
}

public class GenderDistributionDto
{
    public string Gender { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class AgeGroupDistributionDto
{
    public string AgeGroup { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class AuditLogViewModel
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AuditLogDto
{
    public int AuditId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
