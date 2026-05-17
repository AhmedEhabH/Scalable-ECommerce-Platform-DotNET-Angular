using ECommerce.Application.Admin.DTOs;

namespace ECommerce.Application.Admin.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<AnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<TopProductDto>> GetTopProductsAsync(int count = 5, CancellationToken cancellationToken = default);
    Task<List<SalesTrendDto>> GetSalesTrendAsync(int year, CancellationToken cancellationToken = default);
}