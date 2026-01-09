using MediatR;
using ApiGatewayKit.Core.Application.DTOs.Bill;

namespace ApiGatewayKit.Core.Application.Features.Bill.Commands;

/// <summary>
/// Fatura Ã¶deme komutu
/// </summary>
public class PayBillCommand : IRequest<PayBillResponse>
{
    public string? BillId { get; set; }
    public string? SubscriberId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public PaymentCardInfo? Card { get; set; }
}

/// <summary>
/// Ã–deme yanÄ±tÄ±
/// </summary>
public class PayBillResponse
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? Message { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ErrorCode { get; set; }
}

