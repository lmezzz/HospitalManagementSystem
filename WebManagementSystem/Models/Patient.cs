using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class Patient
{
    public int PatientId { get; set; }

    public string? FullName { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Cnic { get; set; }

    public string? Address { get; set; }

    public string? Allergies { get; set; }

    public string? ChronicConditions { get; set; }

    public string? EmergencyContact { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<LabOrder> LabOrders { get; set; } = new List<LabOrder>();

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
