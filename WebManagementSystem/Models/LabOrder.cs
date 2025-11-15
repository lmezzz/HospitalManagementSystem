using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class LabOrder
{
    public int LabOrderId { get; set; }

    public int? VisitId { get; set; }

    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    public int? LabTestId { get; set; }

    public string? Priority { get; set; }

    public string? Status { get; set; }

    public DateTime? OrderTime { get; set; }

    public DateTime? SampleTime { get; set; }

    public DateTime? CompletedTime { get; set; }

    public virtual AppUser? Doctor { get; set; }

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual LabTest? LabTest { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual Visit? Visit { get; set; }
}
