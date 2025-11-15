using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int? DoctorId { get; set; }

    public DateOnly SlotDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual AppUser? Doctor { get; set; }
}
