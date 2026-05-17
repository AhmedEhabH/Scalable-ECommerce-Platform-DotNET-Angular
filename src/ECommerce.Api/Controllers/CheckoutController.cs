using ECommerce.Api.Controllers;
using ECommerce.Api.Models;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Checkout endpoints
/// </summary>
[Authorize]
[EnableRateLimiting("global")]
public class CheckoutController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutController(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Process checkout - creates an order from the user's cart
    /// </summary>
    /// <param name="request">Shipping and billing information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created order</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Validation error or empty cart</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not Guid userId)
            return HandleUnauthorized("User not authenticated");

        var result = await _orderService.CreateOrderAsync(userId, request, cancellationToken);
        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Checkout failed");
        return HandleCreated(result.Value);
    }
}