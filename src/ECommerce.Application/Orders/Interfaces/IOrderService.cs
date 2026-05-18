using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Interfaces;

public interface IOrderService
{
    Task<Result<OrderDto>> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<OrderDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> UpdateOrderStatusAsync(Guid orderId, Guid userId, OrderStatus newStatus, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> AdminUpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}