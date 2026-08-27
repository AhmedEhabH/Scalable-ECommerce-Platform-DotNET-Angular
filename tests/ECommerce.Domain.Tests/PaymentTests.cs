using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ECommerce.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void AuthorizeThenPay_UsesValidLifecycle()
    {
        var payment = CreatePayment();

        payment.Authorize("txn-1");
        payment.MarkAsPaid();

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.IsSuccessful.Should().BeTrue();
        payment.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Authorize_WhenPaymentIsAlreadyPaid_IsRejected()
    {
        var payment = CreatePayment();
        payment.MarkAsPaid();

        var act = () => payment.Authorize("txn-2");

        act.Should().Throw<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void Refund_WhenPaymentIsNotPaid_IsRejected()
    {
        var payment = CreatePayment();

        var act = () => payment.MarkAsRefunded();

        act.Should().Throw<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Theory]
    [InlineData(25, PaymentStatus.PartiallyRefunded)]
    [InlineData(100, PaymentStatus.Refunded)]
    public void Refund_PaidPayment_TracksPartialAndFullRefunds(
        decimal refundAmount,
        PaymentStatus expectedStatus)
    {
        var payment = CreatePayment();
        payment.MarkAsPaid();

        payment.MarkAsRefunded(refundAmount);

        payment.Status.Should().Be(expectedStatus);
        payment.IsRefunded.Should().BeTrue();
    }

    private static Payment CreatePayment() =>
        Payment.Create(Guid.NewGuid(), PaymentMethod.CreditCard, "TestProvider", 100m);
}
