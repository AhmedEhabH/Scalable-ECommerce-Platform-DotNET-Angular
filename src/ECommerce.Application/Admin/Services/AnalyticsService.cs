using ECommerce.Application.Admin.DTOs;
using ECommerce.Application.Admin.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Admin.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public AnalyticsService(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAsync(cancellationToken);
        var topProducts = await GetTopProductsAsync(5, cancellationToken);
        var salesTrend = await GetSalesTrendAsync(DateTime.UtcNow.Year, cancellationToken);

        return new AnalyticsDto(summary, topProducts, salesTrend);
    }

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders.ToListAsync(cancellationToken);
        int totalOrders = orders.Count;
        decimal totalRevenue = orders.Sum(o => o.TotalAmount);
        int totalProducts = await _context.Products.CountAsync(cancellationToken);
        int totalUsers = await _userService.GetTotalUsersCountAsync(cancellationToken);
        decimal avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        return new AnalyticsSummaryDto(
            totalUsers,
            totalOrders,
            totalRevenue,
            totalProducts,
            avgOrderValue);
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        var productSales = await _context.OrderItems
            .Join(_context.Orders.Where(o => o.Status != Domain.Enums.OrderStatus.Cancelled),
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item })
            .Select(x => x.item)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.Total)
            })
            .OrderByDescending(p => p.TotalSold)
            .Take(count)
            .ToListAsync(cancellationToken);

        var productIds = productSales.Select(p => p.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return productSales.Select(p => new TopProductDto(
            p.ProductId,
            products.GetValueOrDefault(p.ProductId, "Unknown Product"),
            p.TotalSold,
            p.TotalRevenue)).ToList();
    }

    public async Task<List<SalesTrendDto>> GetSalesTrendAsync(int year, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Where(o => o.CreatedAt.Year == year && o.Status != Domain.Enums.OrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var monthNames = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var trend = Enumerable.Range(1, 12)
            .Select(month => new SalesTrendDto(
                month,
                monthNames[month],
                orders.Count(o => o.CreatedAt.Month == month),
                orders.Where(o => o.CreatedAt.Month == month).Sum(o => o.TotalAmount)))
            .ToList();

        return trend;
    }
}