using MediatR;
using ApiGatewayKit.Core.Application.DTOs.Bill;

namespace ApiGatewayKit.Core.Application.Features.Bill.Queries;

/// <summary>
/// GÃ¼ncel fatura sorgulama
/// </summary>
public class GetCurrentBillQuery : IRequest<GetCurrentBillResponse>
{
    public string? SubscriberId { get; set; }
    public string? CustomerNo { get; set; }
}

/// <summary>
/// GÃ¼ncel fatura yanÄ±tÄ±
/// </summary>
public class GetCurrentBillResponse
{
    public bool Success { get; set; }
    public BillDto? Bill { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
}

