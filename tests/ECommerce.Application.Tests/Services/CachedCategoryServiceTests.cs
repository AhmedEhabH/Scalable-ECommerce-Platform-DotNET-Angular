using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Categories.Interfaces;
using ECommerce.Application.Categories.Services;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Services;

public class CachedCategoryServiceTests
{
    private readonly Mock<ICategoryService> _innerServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<CachedCategoryService>> _loggerMock;
    private readonly CachedCategoryService _sut;

    public CachedCategoryServiceTests()
    {
        _innerServiceMock = new Mock<ICategoryService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<CachedCategoryService>>();
        _sut = new CachedCategoryService(
            _innerServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    private static CategoryDto CreateCategory(string name) => new(
        Guid.NewGuid(),
        name,
        name.ToLowerInvariant().Replace(" ", "-"),
        "Test description",
        null,
        null,
        true,
        1,
        Array.Empty<CategoryDto>(),
        0,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    [Fact]
    public async Task GetAllAsync_WhenRedisThrowsException_MustReturnDataFromDatabase_WithoutFailing()
    {
        _cacheServiceMock
            .Setup(c => c.GetAsync<Result<IReadOnlyList<CategoryDto>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis connection failed"));

        var dbCategories = new List<CategoryDto> { CreateCategory("Electronics"), CreateCategory("Clothing") };
        _innerServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<CategoryDto>>.Success(dbCategories));

        var result = await _sut.GetAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        _innerServiceMock.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheReturnsData_ReturnsFromCache()
    {
        var cachedCategories = new List<CategoryDto> { CreateCategory("CachedCategory") };
        var cachedResult = Result<IReadOnlyList<CategoryDto>>.Success(cachedCategories);

        _cacheServiceMock
            .Setup(c => c.GetAsync<Result<IReadOnlyList<CategoryDto>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var result = await _sut.GetAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(1);
        _innerServiceMock.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheWriteFails_StillReturnsDatabaseData()
    {
        _cacheServiceMock
            .Setup(c => c.GetAsync<Result<IReadOnlyList<CategoryDto>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<IReadOnlyList<CategoryDto>>?)null);

        var dbCategories = new List<CategoryDto> { CreateCategory("DbCategory") };
        _innerServiceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<CategoryDto>>.Success(dbCategories));

        _cacheServiceMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Result<IReadOnlyList<CategoryDto>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis write failed"));

        var result = await _sut.GetAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRedisThrowsException_FallsBackToDatabase()
    {
        var categoryId = Guid.NewGuid();

        _cacheServiceMock
            .Setup(c => c.GetAsync<Result<CategoryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis down"));

        var category = CreateCategory("TestCategory");
        _innerServiceMock
            .Setup(s => s.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CategoryDto>.Success(category));

        var result = await _sut.GetByIdAsync(categoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("TestCategory");
    }
}