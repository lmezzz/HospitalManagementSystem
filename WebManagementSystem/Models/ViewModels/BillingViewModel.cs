namespace WebManagementSystem.Models.ViewModels;

public class BillListViewModel
{
    public List<BillDto> Bills { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string Filter { get; set; } = "All"; // All, Paid, Unpaid, Partial
}

public class BillDto
{
    public int BillId { get; set; }
    public int? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBillViewModel
{
    public int PatientId { get; set; }
    public int? VisitId { get; set; }

    public List<BillItemViewModel> Items { get; set; } = new();

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public List<ServiceItemDto> AvailableServices { get; set; } = new();
}

public class BillItemViewModel
{
    public int? BillItemId { get; set; }
    public string ItemType { get; set; } = string.Empty; // Consultation, Medication, LabTest, Procedure
    public int? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class EditBillViewModel
{
    public int BillId { get; set; }
    public int PatientId { get; set; }

    public List<BillItemViewModel> Items { get; set; } = new();

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BillDetailsViewModel
{
    public int BillId { get; set; }

    // Patient Information
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    // Bill Items
    public List<BillItemDetailDto> Items { get; set; } = new();

    // Amounts
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Payments
    public List<PaymentDetailDto> Payments { get; set; } = new();
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}

public class BillItemDetailDto
{
    public int BillItemId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class PaymentDetailDto
{
    public int PaymentId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentTime { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class CreatePaymentViewModel
{
    public int BillId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Insurance, Online
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }

    // Helper properties
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PreviouslyPaid { get; set; }
    public decimal Balance { get; set; }
}

public class PaymentReceiptViewModel
{
    public int PaymentId { get; set; }
    public int BillId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentTime { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class ServiceItemDto
{
    public string ItemType { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class InvoiceViewModel
{
    public int BillId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    // Hospital Information
    public string HospitalName { get; set; } = "Hospital Management System";
    public string HospitalAddress { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;

    // Patient Information
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;

    // Bill Details
    public List<BillItemDetailDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Payment Details
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BillSummaryReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalBills { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<BillDto> Bills { get; set; } = new();
}
