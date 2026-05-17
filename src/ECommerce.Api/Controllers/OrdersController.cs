using ECommerce.Api.Controllers;
using ECommerce.Api.Models;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

[Authorize]
[EnableRateLimiting("global")]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not Guid userId)
            return HandleUnauthorized("User not authenticated");

        var result = await _orderService.CreateOrderAsync(userId, request, cancellationToken);
        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Failed to create order");
        return HandleCreated(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<OrderDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not Guid userId)
            return HandleUnauthorized("User not authenticated");

        var result = await _orderService.GetUserOrdersAsync(userId, cancellationToken);
        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Failed to retrieve orders");
        return HandleSuccess(result.Value);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetOrderDetails(Guid orderId, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not Guid userId)
            return HandleUnauthorized("User not authenticated");

        var result = await _orderService.GetOrderByIdAsync(orderId, userId, cancellationToken);
        if (result.IsFailure)
            return HandleNotFound(result.Error ?? "Order not found");
        return HandleSuccess(result.Value);
    }
}