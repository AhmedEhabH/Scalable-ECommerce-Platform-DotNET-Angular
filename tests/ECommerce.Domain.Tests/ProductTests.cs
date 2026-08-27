using ECommerce.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ECommerce.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void DiscountPercentage_WithNoCompareAtPrice_IsZero()
    {
        var product = CreateProduct(price: 80m, compareAtPrice: null);

        product.HasDiscount.Should().BeFalse();
        product.DiscountPercentage.Should().Be(0m);
    }

    [Theory]
    [InlineData(80, 100, 20)]
    [InlineData(100, 100, 0)]
    [InlineData(120, 100, 0)]
    public void DiscountPercentage_ReflectsOnlyARealPriceReduction(
        decimal price,
        decimal compareAtPrice,
        decimal expectedPercentage)
    {
        var product = CreateProduct(price, compareAtPrice);

        product.DiscountPercentage.Should().Be(expectedPercentage);
    }

    [Fact]
    public void UpdateStock_WhenResultWouldBeNegative_RejectsChange()
    {
        var product = CreateProduct(stockQuantity: 3);

        var act = () => product.UpdateStock(-4);

        act.Should().Throw<InvalidOperationException>();
        product.StockQuantity.Should().Be(3);
    }

    [Fact]
    public void SetStock_WhenQuantityIsNegative_RejectsChange()
    {
        var product = CreateProduct(stockQuantity: 3);

        var act = () => product.SetStock(-1);

        act.Should().Throw<ArgumentException>();
        product.StockQuantity.Should().Be(3);
    }

    private static Product CreateProduct(
        decimal price = 80m,
        decimal? compareAtPrice = null,
        int stockQuantity = 10) =>
        Product.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "test-product",
            price,
            "TEST-001",
            stockQuantity,
            compareAtPrice: compareAtPrice);
}
