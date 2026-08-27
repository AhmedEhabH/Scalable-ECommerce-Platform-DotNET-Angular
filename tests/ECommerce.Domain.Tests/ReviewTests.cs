using ECommerce.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ECommerce.Domain.Tests;

public class ReviewTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WhenRatingIsOutsideOneToFive_RejectsRating(int rating)
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.NewGuid(), rating, "Title");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidRatingAndText_ChangesReview()
    {
        var review = Review.Create(Guid.NewGuid(), Guid.NewGuid(), 3, "Original", "Original comment");

        review.Update(5, " Updated ", " Better ");

        review.Rating.Should().Be(5);
        review.Title.Should().Be("Updated");
        review.Comment.Should().Be("Better");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Update_WhenRatingIsOutsideOneToFive_PreservesReview(int rating)
    {
        var review = Review.Create(Guid.NewGuid(), Guid.NewGuid(), 3, "Original");

        var act = () => review.Update(rating, "Changed", null);

        act.Should().Throw<ArgumentException>();
        review.Rating.Should().Be(3);
        review.Title.Should().Be("Original");
    }
}
