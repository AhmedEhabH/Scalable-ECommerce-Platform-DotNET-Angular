using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Products.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IVendorRepository> _vendorRepositoryMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _vendorRepositoryMock = new Mock<IVendorRepository>();
        _loggerMock = new Mock<ILoggerService>();
        _sut = new ProductService(_productRepositoryMock.Object, _categoryRepositoryMock.Object, _vendorRepositoryMock.Object, _loggerMock.Object);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSuccess_WhenProductExists()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(r => r.GetWithImagesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(product.Id);
        result.Value.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(r => r.GetWithImagesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(productId);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    #endregion

    #region GetBySlugAsync

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnSuccess_WhenProductExists()
    {
        var product = CreateProduct();
        var category = CreateCategory();
        _productRepositoryMock.Setup(r => r.GetBySlugAsync(product.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _categoryRepositoryMock.Setup(r => r.GetByIdAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var result = await _sut.GetBySlugAsync(product.Slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Product.Name.Should().Be(product.Name);
        result.Value.CategoryName.Should().Be(category.Name);
        result.Value.CategorySlug.Should().Be(category.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        _productRepositoryMock.Setup(r => r.GetBySlugAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.GetBySlugAsync("nonexistent");

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    #endregion

    #region GetFeaturedAsync

    [Fact]
    public async Task GetFeaturedAsync_ShouldReturnProducts()
    {
        var products = new List<Product> { CreateProduct(), CreateProduct() };
        _productRepositoryMock.Setup(r => r.GetFeaturedAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _sut.GetFeaturedAsync(10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region GetByCategoryAsync

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnProducts_WhenCategoryExists()
    {
        var categoryId = Guid.NewGuid();
        var products = new List<Product> { CreateProduct(categoryId: categoryId) };
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetByCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _sut.GetByCategoryAsync(categoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetByCategoryAsync(categoryId);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("CATEGORY_NOT_FOUND");
    }

    #endregion

    #region SearchAsync

    [Fact]
    public async Task SearchAsync_ShouldReturnProducts_WhenSearchTermIsValid()
    {
        var products = new List<Product> { CreateProduct() };
        _productRepositoryMock.Setup(r => r.SearchAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _sut.SearchAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFailure_WhenSearchTermIsEmpty()
    {
        var result = await _sut.SearchAsync("");

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_SEARCH_TERM");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFailure_WhenSearchTermIsWhitespace()
    {
        var result = await _sut.SearchAsync("   ");

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_SEARCH_TERM");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenValidRequest()
    {
        var vendorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest(categoryId, "Test Product", "test-product", 10.00m, "SKU001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetBySKUAsync(request.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.GetBySlugAsync(request.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var result = await _sut.CreateAsync(vendorId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Product");
        result.Value.SKU.Should().Be("SKU001");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenVendorIdIsNull_ForAdminCreatedProduct()
    {
        Guid? vendorId = null;
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest(categoryId, "Admin Product", "admin-product", 10.00m, "ADM001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetBySKUAsync(request.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.GetBySlugAsync(request.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var result = await _sut.CreateAsync(vendorId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Admin Product");
        result.Value.VendorId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenVendorIdIsSet_ForSellerCreatedProduct()
    {
        var vendorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest(categoryId, "Seller Product", "seller-product", 10.00m, "SEL001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetBySKUAsync(request.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.GetBySlugAsync(request.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        var result = await _sut.CreateAsync(vendorId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Seller Product");
        result.Value.VendorId.Should().Be(vendorId);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        var request = new CreateProductRequest(Guid.NewGuid(), "Test", "test", 10.00m, "SKU001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(request.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("CATEGORY_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenSkuAlreadyExists()
    {
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest(categoryId, "Test", "test", 10.00m, "SKU001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetBySKUAsync(request.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct(sku: "SKU001"));

        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_SKU_EXISTS");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenSlugAlreadyExists()
    {
        var categoryId = Guid.NewGuid();
        var request = new CreateProductRequest(categoryId, "Test", "test-slug", 10.00m, "SKU001", 100);
        _categoryRepositoryMock.Setup(r => r.ExistsAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock.Setup(r => r.GetBySKUAsync(request.SKU, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(r => r.GetBySlugAsync(request.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct(slug: "test-slug"));

        var result = await _sut.CreateAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_SLUG_EXISTS");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenProductExists()
    {
        var product = CreateProduct();
        var request = new UpdateProductRequest("Updated Name");
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(product.Id, request, null, true, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.UpdateAsync(productId, new UpdateProductRequest(), null, true, false);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenSellerAttemptsToModifyOthersProduct()
    {
        var sellerUserId = Guid.NewGuid();
        var sellerVendorId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: otherVendorId);
        var request = new UpdateProductRequest("Hacked Name");

        var vendor = Vendor.Create(sellerUserId, "Test Vendor", null, null, "test@test.com");
        typeof(BaseEntity).GetProperty("Id")?.SetValue(vendor, sellerVendorId);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        var result = await _sut.UpdateAsync(product.Id, request, sellerUserId, false, true);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenSellerModifiesOwnProduct()
    {
        var sellerUserId = Guid.NewGuid();
        var sellerVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: sellerVendorId);
        var request = new UpdateProductRequest("My Updated Product");

        var vendor = Vendor.Create(sellerUserId, "Test Vendor", null, null, "test@test.com");
        typeof(BaseEntity).GetProperty("Id")?.SetValue(vendor, sellerVendorId);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        var result = await _sut.UpdateAsync(product.Id, request, sellerUserId, false, true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Updated Product");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenAdminModifiesAnyProduct()
    {
        var adminUserId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: otherVendorId);
        var request = new UpdateProductRequest("Admin Updated Product");

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(product.Id, request, adminUserId, true, false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Admin Updated Product");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenSellerHasNoVendorProfile()
    {
        var sellerUserId = Guid.NewGuid();
        var product = CreateProduct();
        var request = new UpdateProductRequest("Hacked Name");

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor?)null);

        var result = await _sut.UpdateAsync(product.Id, request, sellerUserId, false, true);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenProductExists()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(product.Id, null, true, false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(productId, null, true, false);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenSellerAttemptsToDeleteOthersProduct()
    {
        var sellerUserId = Guid.NewGuid();
        var sellerVendorId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: otherVendorId);

        var vendor = Vendor.Create(sellerUserId, "Test Vendor", null, null, "test@test.com");
        typeof(BaseEntity).GetProperty("Id")?.SetValue(vendor, sellerVendorId);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        var result = await _sut.DeleteAsync(product.Id, sellerUserId, false, true);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenSellerDeletesOwnProduct()
    {
        var sellerUserId = Guid.NewGuid();
        var sellerVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: sellerVendorId);

        var vendor = Vendor.Create(sellerUserId, "Test Vendor", null, null, "test@test.com");
        typeof(BaseEntity).GetProperty("Id")?.SetValue(vendor, sellerVendorId);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        var result = await _sut.DeleteAsync(product.Id, sellerUserId, false, true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenAdminDeletesAnyProduct()
    {
        var adminUserId = Guid.NewGuid();
        var otherVendorId = Guid.NewGuid();
        var product = CreateProduct(vendorId: otherVendorId);

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _productRepositoryMock.Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(product.Id, adminUserId, true, false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenSellerHasNoVendorProfile()
    {
        var sellerUserId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _vendorRepositoryMock.Setup(r => r.GetByUserIdAsync(sellerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor?)null);

        var result = await _sut.DeleteAsync(product.Id, sellerUserId, false, true);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("FORBIDDEN");
    }

    #endregion

    #region ToggleActiveAsync

    [Fact]
    public async Task ToggleActiveAsync_ShouldDeactivate_WhenProductIsActive()
    {
        var product = CreateProduct(isActive: true);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.ToggleActiveAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleActiveAsync_ShouldActivate_WhenProductIsInactive()
    {
        var product = CreateProduct(isActive: false);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.ToggleActiveAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleActiveAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.ToggleActiveAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    #endregion

    #region ToggleFeaturedAsync

    [Fact]
    public async Task ToggleFeaturedAsync_ShouldRemoveFromFeatured_WhenProductIsFeatured()
    {
        var product = CreateProduct(isFeatured: true);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.ToggleFeaturedAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleFeaturedAsync_ShouldSetAsFeatured_WhenProductIsNotFeatured()
    {
        var product = CreateProduct(isFeatured: false);
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.ToggleFeaturedAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        product.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleFeaturedAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.ToggleFeaturedAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    #endregion

    #region AddImageAsync

    [Fact]
    public async Task AddImageAsync_ShouldReturnSuccess_WhenValidInput()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.AddImageAsync(product.Id, "https://example.com/img.jpg", "Test Image", 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddImageAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.AddImageAsync(Guid.NewGuid(), "https://example.com/img.jpg", "Test", 1);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task AddImageAsync_ShouldReturnFailure_WhenImageUrlIsEmpty()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.AddImageAsync(product.Id, "", "Test", 1);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_IMAGE_URL");
    }

    [Fact]
    public async Task AddImageAsync_ShouldReturnFailure_WhenImageUrlIsWhitespace()
    {
        var product = CreateProduct();
        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.AddImageAsync(product.Id, "   ", "Test", 1);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("INVALID_IMAGE_URL");
    }

    #endregion

    #region MainImageUrl Mapping

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMainImageUrl_FromFirstActiveImage()
    {
        var product = CreateProduct();
        product.AddImage("https://example.com/first.jpg", "First", 1);
        product.AddImage("https://example.com/second.jpg", "Second", 2);
        product.AddImage("https://example.com/third.jpg", "Third", 3);
        var firstImage = product.Images.First();
        typeof(BaseEntity).GetProperty("IsActive")?.SetValue(firstImage, false);

        _productRepositoryMock.Setup(r => r.GetWithImagesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.MainImageUrl.Should().Be("https://example.com/second.jpg");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullMainImageUrl_WhenNoActiveImages()
    {
        var product = CreateProduct();
        product.AddImage("https://example.com/inactive.jpg", "Inactive", 1);
        var image = product.Images.First();
        typeof(BaseEntity).GetProperty("IsActive")?.SetValue(image, false);

        _productRepositoryMock.Setup(r => r.GetWithImagesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.MainImageUrl.Should().BeNull();
    }

    #endregion

    #region RemoveImageAsync

    [Fact]
    public async Task RemoveImageAsync_ShouldReturnSuccess_WhenProductExists()
    {
        var product = CreateProduct();
        var imageId = Guid.NewGuid();
        product.AddImage("https://example.com/img.jpg", "Test", 1);
        var addedImage = product.Images.First();

        _productRepositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.RemoveImageAsync(product.Id, addedImage.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveImageAsync_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        _productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.RemoveImageAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
    }

    #endregion

    #region Helper Methods

    private static Product CreateProduct(
        Guid? id = null,
        Guid? vendorId = null,
        Guid? categoryId = null,
        string? name = null,
        string? slug = null,
        string? sku = null,
        bool isFeatured = false,
        bool isActive = true)
    {
        var product = Product.Create(
            vendorId ?? Guid.NewGuid(),
            categoryId ?? Guid.NewGuid(),
            name ?? "Test Product",
            slug ?? "test-product",
            10.00m,
            sku ?? "SKU001",
            100
        );

        if (isFeatured) product.SetAsFeatured();
        if (!isActive) product.Deactivate();

        if (id.HasValue)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))?.SetValue(product, id.Value);
        }

        return product;
    }

    private static Category CreateCategory(
        Guid? id = null,
        string? name = null,
        string? slug = null)
    {
        var category = Category.Create(
            name ?? "Test Category",
            slug ?? "test-category"
        );

        if (id.HasValue)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))?.SetValue(category, id.Value);
        }

        return category;
    }

    #endregion
}
