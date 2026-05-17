using ECommerce.Application.Admin.DTOs;
using ECommerce.Application.Admin.Interfaces;
using ECommerce.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalytics([FromQuery] int year = 2026)
    {
        var analytics = await _analyticsService.GetAnalyticsAsync(year);
        return Ok(ApiResponse<AnalyticsDto>.SuccessResponse(analytics));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var summary = await _analyticsService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AnalyticsSummaryDto>.SuccessResponse(summary));
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int count = 5, CancellationToken cancellationToken = default)
    {
        var products = await _analyticsService.GetTopProductsAsync(count, cancellationToken);
        return Ok(ApiResponse<List<TopProductDto>>.SuccessResponse(products));
    }

    [HttpGet("sales-trend")]
    public async Task<IActionResult> GetSalesTrend([FromQuery] int year = 2026, CancellationToken cancellationToken = default)
    {
        var trend = await _analyticsService.GetSalesTrendAsync(year, cancellationToken);
        return Ok(ApiResponse<List<SalesTrendDto>>.SuccessResponse(trend));
    }
}