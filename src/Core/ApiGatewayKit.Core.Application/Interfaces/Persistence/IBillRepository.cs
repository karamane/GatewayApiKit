using ApiGatewayKit.Core.Application.DTOs.Bill;

namespace ApiGatewayKit.Core.Application.Interfaces.Persistence;

/// <summary>
/// Fatura repository interface
/// </summary>
public interface IBillRepository
{
    /// <summary>
    /// GÃ¼ncel fatura
    /// </summary>
    Task<BillDto?> GetCurrentBillAsync(string subscriberId, CancellationToken ct = default);

    /// <summary>
    /// Ã–denmemiÅŸ faturalar
    /// </summary>
    Task<List<BillDto>> GetUnpaidBillsAsync(string subscriberId, CancellationToken ct = default);

    /// <summary>
    /// Fatura detayÄ±
    /// </summary>
    Task<BillDto?> GetBillByIdAsync(string billId, CancellationToken ct = default);

    /// <summary>
    /// Fatura Ã¶deme kaydÄ±
    /// </summary>
    Task<bool> RecordPaymentAsync(string billId, decimal amount, string transactionId, CancellationToken ct = default);
}

