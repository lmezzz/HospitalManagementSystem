using System;
using System.Collections.Generic;

namespace WebManagementSystem;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? BillId { get; set; }

    public decimal? AmountPaid { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentTime { get; set; }

    public virtual Bill? Bill { get; set; }
}
