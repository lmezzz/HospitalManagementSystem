namespace WebManagementSystem.Models.ViewModels;

public class AppointmentListViewModel
{
    public List<AppointmentDto> Appointments { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int? DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime? ScheduledTime { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateAppointmentViewModel
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan ScheduledTime { get; set; }
    public int? ScheduleId { get; set; } // Selected schedule slot ID
    public string Reason { get; set; } = string.Empty;

    // Helper properties for UI
    public List<PatientSelectDto> AvailablePatients { get; set; } = new();
    public List<DoctorSelectDto> AvailableDoctors { get; set; } = new();
}

public class EditAppointmentViewModel
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan ScheduledTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class AppointmentDetailsViewModel
{
    public int AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Visit information if exists
    public int? VisitId { get; set; }
    public DateTime? VisitTime { get; set; }
    public string? Diagnosis { get; set; }
}

public class DoctorScheduleViewModel
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<ScheduleSlotDto> Slots { get; set; } = new();
}

public class ScheduleSlotDto
{
    public int? ScheduleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int? AppointmentId { get; set; }
    public string? PatientName { get; set; }
}

public class CreateScheduleViewModel
{
    public int DoctorId { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class AvailableSlotsViewModel
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<TimeSlotDto> AvailableSlots { get; set; } = new();
}

public class TimeSlotDto
{
    public int ScheduleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string DisplayTime { get; set; } = string.Empty;
}

public class PatientSelectDto
{
    public int PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
}

public class DoctorSelectDto
{
    public int DoctorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AppointmentCalendarViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CalendarEventDto> Events { get; set; } = new();
}

public class CalendarEventDto
{
    public int AppointmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
