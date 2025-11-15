using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int? VisitId { get; set; }

    public int? DoctorId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AppUser? Doctor { get; set; }

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();

    public virtual Visit? Visit { get; set; }
}
