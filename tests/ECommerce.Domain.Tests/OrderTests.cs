using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Xunit;
using ShippingAddress = ECommerce.Domain.ValueObjects.Address;

namespace ECommerce.Domain.Tests;

public class OrderTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Refunded)]
    public void UpdateStatus_AllowsDocumentedTransition(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrderAt(from);

        order.UpdateStatus(to);

        order.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Refunded, OrderStatus.Processing)]
    public void UpdateStatus_RejectsInvalidTransition(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrderAt(from);

        var act = () => order.UpdateStatus(to);

        act.Should().Throw<InvalidOperationException>();
        order.Status.Should().Be(from);
    }

    private static Order CreateOrderAt(OrderStatus status)
    {
        var order = Order.Create(
            Guid.NewGuid(),
            100m,
            10m,
            5m,
            0m,
            ShippingAddress.Create("1 Main Street", "Cairo", "Cairo", "11511", "Egypt"));

        if (status is OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Refunded)
            order.Confirm();
        if (status is OrderStatus.Processing or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Refunded)
            order.StartProcessing();
        if (status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Refunded)
            order.Ship();
        if (status is OrderStatus.Delivered or OrderStatus.Refunded)
            order.Deliver();
        if (status is OrderStatus.Refunded)
            order.Refund();
        if (status is OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }
}
