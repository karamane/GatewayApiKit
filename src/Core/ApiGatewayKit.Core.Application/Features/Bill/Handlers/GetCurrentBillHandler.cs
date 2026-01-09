using MediatR;
using Microsoft.Extensions.Logging;
using ApiGatewayKit.Core.Application.Features.Bill.Queries;
using ApiGatewayKit.Core.Application.Interfaces.Persistence;

namespace ApiGatewayKit.Core.Application.Features.Bill.Handlers;

/// <summary>
/// GÃ¼ncel fatura sorgulama handler
/// </summary>
public class GetCurrentBillHandler : IRequestHandler<GetCurrentBillQuery, GetCurrentBillResponse>
{
    private readonly IBillRepository _billRepository;
    private readonly ILogger<GetCurrentBillHandler> _logger;

    public GetCurrentBillHandler(
        IBillRepository billRepository,
        ILogger<GetCurrentBillHandler> logger)
    {
        _billRepository = billRepository;
        _logger = logger;
    }

    public async Task<GetCurrentBillResponse> Handle(
        GetCurrentBillQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GÃ¼ncel fatura sorgusu: {SubscriberId}", request.SubscriberId);

        try
        {
            var bill = await _billRepository.GetCurrentBillAsync(
                request.SubscriberId ?? request.CustomerNo ?? "",
                cancellationToken);

            if (bill == null)
            {
                return new GetCurrentBillResponse
                {
                    Success = true,
                    Message = "GÃ¼ncel fatura bulunamadÄ±"
                };
            }

            return new GetCurrentBillResponse
            {
                Success = true,
                Bill = bill
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GÃ¼ncel fatura sorgulama hatasÄ±");
            return new GetCurrentBillResponse
            {
                Success = false,
                Message = "Fatura sorgulanamadÄ±",
                ErrorCode = "BILL_QUERY_ERROR"
            };
        }
    }
}

