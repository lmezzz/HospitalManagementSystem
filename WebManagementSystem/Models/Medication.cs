using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class Medication
{
    public int MedicationId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? UnitPrice { get; set; }

    public int? StockQuantity { get; set; }

    public int? LowStockThreshold { get; set; }

    public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
