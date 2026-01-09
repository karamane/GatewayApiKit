namespace ApiGatewayKit.Core.Application.DTOs.Bill;

/// <summary>
/// Fatura DTO
/// </summary>
public class BillDto
{
    public string? BillId { get; set; }
    public string? BillNo { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? BillDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public string? Period { get; set; }
    public bool IsPaid { get; set; }
}

/// <summary>
/// Kart bilgileri
/// </summary>
public class PaymentCardInfo
{
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? Cvv { get; set; }
}



