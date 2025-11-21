using System;
using System.Collections.Generic;

namespace WebManagementSystem;

public partial class LabResult
{
    public int LabResultId { get; set; }

    public int? LabOrderId { get; set; }

    public string? ResultText { get; set; }

    public string? FilePath { get; set; }

    public int? UploadedBy { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual LabOrder? LabOrder { get; set; }

    public virtual AppUser? UploadedByNavigation { get; set; }
}
