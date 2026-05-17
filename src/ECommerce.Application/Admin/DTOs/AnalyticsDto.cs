namespace ECommerce.Application.Admin.DTOs;

public record AnalyticsSummaryDto(
    int TotalUsers,
    int TotalOrders,
    decimal TotalRevenue,
    int TotalProducts,
    decimal AverageOrderValue);

public record TopProductDto(
    Guid ProductId,
    string ProductName,
    int TotalSold,
    decimal TotalRevenue);

public record SalesTrendDto(
    int Month,
    string MonthName,
    int OrderCount,
    decimal Revenue);

public record AnalyticsDto(
    AnalyticsSummaryDto Summary,
    List<TopProductDto> TopProducts,
    List<SalesTrendDto> SalesTrend);