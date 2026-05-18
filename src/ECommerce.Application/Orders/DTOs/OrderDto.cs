namespace ECommerce.Application.Orders.DTOs;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal ShippingCost,
    decimal DiscountAmount,
    decimal TotalAmount,
    AddressDto? ShippingAddress,
    AddressDto? BillingAddress,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<OrderItemDto> Items,
    int TotalItems,
    Guid? PaymentId,
    string? PaymentStatus,
    string? UserEmail = null
);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SKU,
    decimal Price,
    int Quantity,
    decimal Discount,
    decimal Total
);

public record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country
);