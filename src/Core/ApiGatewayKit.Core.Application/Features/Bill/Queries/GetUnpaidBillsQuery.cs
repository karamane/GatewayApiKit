using MediatR;
using ApiGatewayKit.Core.Application.DTOs.Bill;

namespace ApiGatewayKit.Core.Application.Features.Bill.Queries;

/// <summary>
/// Ã–denmemiÅŸ faturalar sorgulama
/// </summary>
public class GetUnpaidBillsQuery : IRequest<GetUnpaidBillsResponse>
{
    public string? SubscriberId { get; set; }
    public string? CustomerNo { get; set; }
}

/// <summary>
/// Ã–denmemiÅŸ faturalar yanÄ±tÄ±
/// </summary>
public class GetUnpaidBillsResponse
{
    public bool Success { get; set; }
    public List<BillDto> Bills { get; set; } = new();
    public decimal TotalUnpaid { get; set; }
    public int UnpaidCount { get; set; }
    public string? Message { get; set; }
}

