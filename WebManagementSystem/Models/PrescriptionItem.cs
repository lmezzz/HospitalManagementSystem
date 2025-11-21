using System;
using System.Collections.Generic;

namespace WebManagementSystem;

public partial class PrescriptionItem
{
    public int PrescriptionItemId { get; set; }

    public int? PrescriptionId { get; set; }

    public int? MedicationId { get; set; }

    public string? Dosage { get; set; }

    public string? Frequency { get; set; }

    public string? Duration { get; set; }

    public int? Quantity { get; set; }

    public virtual Medication? Medication { get; set; }

    public virtual Prescription? Prescription { get; set; }
}
