using ECommerce.Application.Cart.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.DTOs;
using ECommerce.Application.Orders.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using VOAddress = ECommerce.Domain.ValueObjects.Address;

namespace ECommerce.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public OrderService(IApplicationDbContext context, ICartService cartService, IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _cartService = cartService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var cartEntity = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive, cancellationToken);

        if (cartEntity == null || cartEntity.Items.Count == 0)
            return Result<OrderDto>.Failure("Cart is empty");

        var cartItems = cartEntity.Items.ToList();

        var stockErrors = new List<string>();
        foreach (var item in cartItems)
        {
            if (item.Product == null || !item.Product.IsActive)
            {
                stockErrors.Add($"Product not found or inactive: {item.ProductId}");
                continue;
            }

            if (item.Product.StockQuantity < item.Quantity)
            {
                stockErrors.Add($"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.StockQuantity}, Requested: {item.Quantity}");
            }
        }

        if (stockErrors.Count > 0)
            return Result<OrderDto>.Failure(string.Join("; ", stockErrors));

        var subtotal = cartItems.Sum(ci => ci.UnitPrice * ci.Quantity);
        var taxRate = 0.1m;
        var taxAmount = Math.Round(subtotal * taxRate, 2);
        var shippingCost = subtotal > 100 ? 0 : 10m;
        var discountAmount = 0m;

        var shippingAddress = VOAddress.Create(
            request.ShippingAddress.Street,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Country
        );

        VOAddress? billingAddress = request.BillingAddress != null
            ? VOAddress.Create(
                request.BillingAddress.Street,
                request.BillingAddress.City,
                request.BillingAddress.State,
                request.BillingAddress.PostalCode,
                request.BillingAddress.Country
            )
            : null;

        var executionStrategy = _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = Order.Create(
                    userId,
                    subtotal,
                    taxAmount,
                    shippingCost,
                    discountAmount,
                    shippingAddress,
                    billingAddress,
                    request.Notes
                );

                foreach (var item in cartItems)
                {
                    if (item.Product != null)
                    {
                        order.AddItem(
                            item.ProductId,
                            item.Product.VendorId ?? Guid.Empty,
                            item.Product.Name,
                            item.Product.SKU,
                            item.UnitPrice,
                            item.Quantity
                        );

                        item.Product.UpdateStock(-item.Quantity);
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);

                cartEntity.Clear();
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return Result<OrderDto>.Success(MapToOrderDto(order));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient stock"))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<OrderDto>.Failure(ex.Message);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<Result<OrderDto>> GetOrderByIdAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return Result<OrderDto>.Failure("Order not found");

        return Result<OrderDto>.Success(MapToOrderDto(order));
    }

    public async Task<Result<IReadOnlyCollection<OrderDto>>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderDtos = orders.Select(MapToOrderDto).ToList();
        return Result<IReadOnlyCollection<OrderDto>>.Success(orderDtos);
    }

    public async Task<Result<OrderDto>> UpdateOrderStatusAsync(Guid orderId, Guid userId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return Result<OrderDto>.Failure("Order not found");

        try
        {
            var previousStatus = order.Status;
            order.UpdateStatus(newStatus);
            await _context.SaveChangesAsync(cancellationToken);

            var userEmail = order.User!.Email;
            _backgroundJobClient.Enqueue<IEmailService>(emailService =>
                emailService.SendEmailAsync(userEmail, "Order Status Update",
                    $"Your order status is now {newStatus}", CancellationToken.None));

            return Result<OrderDto>.Success(MapToOrderDto(order));
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<OrderDto>> AdminUpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            return Result<OrderDto>.Failure("Order not found");

        try
        {
            order.UpdateStatus(newStatus);
            await _context.SaveChangesAsync(cancellationToken);

            var userEmail = order.User!.Email;
            _backgroundJobClient.Enqueue<IEmailService>(emailService =>
                emailService.SendEmailAsync(userEmail, "Order Status Update",
                    $"Your order status is now {newStatus}", CancellationToken.None));

            return Result<OrderDto>.Success(MapToOrderDto(order));
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(
            Id: i.Id,
            ProductId: i.ProductId,
            ProductName: i.ProductName,
            SKU: i.SKU,
            Price: i.Price,
            Quantity: i.Quantity,
            Discount: i.Discount,
            Total: i.Total
        )).ToList();

        AddressDto? shippingAddress = null;
        var shippingAddr = order.GetShippingAddress();
        if (shippingAddr != null)
        {
            shippingAddress = new AddressDto(
                shippingAddr.Street,
                shippingAddr.City,
                shippingAddr.State,
                shippingAddr.PostalCode,
                shippingAddr.Country
            );
        }

        AddressDto? billingAddress = null;
        var billingAddr = order.GetBillingAddress();
        if (billingAddr != null)
        {
            billingAddress = new AddressDto(
                billingAddr.Street,
                billingAddr.City,
                billingAddr.State,
                billingAddr.PostalCode,
                billingAddr.Country
            );
        }

        return new OrderDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            Status: order.Status.ToString(),
            SubTotal: order.SubTotal,
            TaxAmount: order.TaxAmount,
            ShippingCost: order.ShippingCost,
            DiscountAmount: order.DiscountAmount,
            TotalAmount: order.TotalAmount,
            ShippingAddress: shippingAddress,
            BillingAddress: billingAddress,
            Notes: order.Notes,
            CreatedAt: order.CreatedAt,
            UpdatedAt: order.UpdatedAt,
            Items: items.AsReadOnly(),
            TotalItems: order.TotalItems,
            PaymentId: order.PaymentId,
            PaymentStatus: order.Payment?.Status.ToString()
        );
    }
}
