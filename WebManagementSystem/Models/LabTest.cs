using System;
using System.Collections.Generic;

namespace WebManagementSystem.Models;

public partial class LabTest
{
    public int LabTestId { get; set; }

    public string? TestName { get; set; }

    public string? Category { get; set; }

    public decimal? Cost { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<LabOrder> LabOrders { get; set; } = new List<LabOrder>();
}
