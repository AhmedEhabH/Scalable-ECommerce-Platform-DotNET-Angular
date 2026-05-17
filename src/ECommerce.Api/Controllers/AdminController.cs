using ECommerce.Application.Admin.DTOs;
using ECommerce.Application.Admin;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Application.Products.Interfaces;
using ECommerce.Application.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("global")]
public class AdminController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IUserService _userService;
    private readonly IApplicationDbContext _context;

    public AdminController(
        IOrderService orderService,
        IProductService productService,
        IUserService userService,
        IApplicationDbContext context)
    {
        _orderService = orderService;
        _productService = productService;
        _userService = userService;
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);

        int totalOrders = orders.Count;
        decimal totalSales = orders.Sum(o => o.TotalAmount);

        var products = await _context.Products.ToListAsync(cancellationToken);
        int totalProducts = products.Count;

        int totalUsers = await _userService.GetTotalUsersCountAsync(cancellationToken);

        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new ECommerce.Application.Orders.DTOs.OrderDto(
                o.Id,
                o.OrderNumber,
                o.Status.ToString(),
                o.SubTotal,
                o.TaxAmount,
                o.ShippingCost,
                o.DiscountAmount,
                o.TotalAmount,
                null,
                null,
                o.Notes,
                o.CreatedAt,
                o.UpdatedAt,
                new List<ECommerce.Application.Orders.DTOs.OrderItemDto>(),
                o.TotalItems,
                o.PaymentId,
                null))
            .ToList();

        var summary = new AdminDashboardSummaryDto(
            totalOrders,
            totalSales,
            totalProducts,
            totalUsers,
            recentOrders,
            new List<ECommerce.Application.Products.DTOs.ProductDto>(),
            new List<ECommerce.Application.Products.DTOs.ProductDto>());

        return Ok(summary);
    }
}