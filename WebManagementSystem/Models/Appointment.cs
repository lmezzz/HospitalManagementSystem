using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime? ScheduledTime { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser? Doctor { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual Schedule? Schedule { get; set; }

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
