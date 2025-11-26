using System;
using System.Collections.Generic;

namespace WebManagementSystem;

public partial class BillItem
{
    public int BillItemId { get; set; }

    public int? BillId { get; set; }

    public string? ItemType { get; set; }

    public int? ReferenceId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Amount { get; set; }

    public virtual Bill? Bill { get; set; }
}
