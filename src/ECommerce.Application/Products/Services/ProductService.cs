using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Products.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly ILoggerService _logger;

    public ProductService(
        IProductRepository productRepository, 
        ICategoryRepository categoryRepository,
        IVendorRepository vendorRepository,
        ILoggerService logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _vendorRepository = vendorRepository;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetWithImagesAsync(id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            return Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND");
        }

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PaginatedResult<ProductDto>>> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _productRepository.GetQueryable();

        queryable = ApplyFilters(queryable, query);
        queryable = ApplySorting(queryable, query);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var products = await queryable
            .Include(p => p.Images)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = products.Select(MapToDto).ToList();
        var result = new PaginatedResult<ProductDto>(dtos, totalCount, query.Page, query.PageSize);

        _logger.LogDebug("Retrieved {Count} products (page {Page}, total {TotalCount})", dtos.Count, query.Page, totalCount);

        return Result<PaginatedResult<ProductDto>>.Success(result);
    }

    public async Task<Result<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetBySlugAsync(slug, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found by slug: {Slug}", slug);
            return Result<ProductDetailResponse>.Failure("Product not found", "PRODUCT_NOT_FOUND");
        }

        var category = await _categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken);

        var dto = MapToDto(product);
        var response = new ProductDetailResponse(dto, category?.Name, category?.Slug);

        return Result<ProductDetailResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetFeaturedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetFeaturedAsync(count, cancellationToken);
        var dtos = products.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ProductDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(categoryId, cancellationToken);
        if (!categoryExists)
        {
            _logger.LogWarning("Category not found for product query: {CategoryId}", categoryId);
            return Result<IReadOnlyList<ProductDto>>.Failure("Category not found", "CATEGORY_NOT_FOUND");
        }

        var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
        var dtos = products.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ProductDto>>.Success(dtos);
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Result<IReadOnlyList<ProductDto>>.Failure("Search term is required", "INVALID_SEARCH_TERM");

        var products = await _productRepository.SearchAsync(searchTerm, cancellationToken);
        var dtos = products.Select(MapToDto).ToList();

        _logger.LogDebug("Product search: {SearchTerm}, found {Count} results", searchTerm, dtos.Count);

        return Result<IReadOnlyList<ProductDto>>.Success(dtos);
    }

    public async Task<Result<ProductDto>> CreateAsync(Guid? vendorId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            _logger.LogWarning("Failed to create product: category {CategoryId} not found", request.CategoryId);
            return Result<ProductDto>.Failure("Category not found", "CATEGORY_NOT_FOUND");
        }

        var skuExists = await _productRepository.GetBySKUAsync(request.SKU, cancellationToken);
        if (skuExists != null)
        {
            _logger.LogWarning("Failed to create product: SKU {SKU} already exists", request.SKU);
            return Result<ProductDto>.Failure("A product with this SKU already exists", "PRODUCT_SKU_EXISTS");
        }

        var slugExists = await _productRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (slugExists != null)
        {
            _logger.LogWarning("Failed to create product: slug {Slug} already exists", request.Slug);
            return Result<ProductDto>.Failure("A product with this slug already exists", "PRODUCT_SLUG_EXISTS");
        }

        var product = Product.Create(
            vendorId,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Price,
            request.SKU,
            request.StockQuantity,
            request.Description,
            request.CompareAtPrice,
            request.LowStockThreshold,
            request.IsFeatured
        );

        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            product.AddImage(request.ImageUrl);
        }

        await _productRepository.AddAsync(product, cancellationToken);

        _logger.LogInformation("Product created: {ProductId}, Name: {Name}, VendorId: {VendorId}", product.Id, product.Name, vendorId);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Failed to update product: {ProductId} not found", id);
            return Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND");
        }

        if (isSeller && !isAdmin)
        {
            if (currentUserId == null)
            {
                _logger.LogWarning("Seller attempted to update product without user ID");
                return Result<ProductDto>.Failure("User ID is required", "UNAUTHORIZED");
            }

            var vendor = await _vendorRepository.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            if (vendor == null)
            {
                _logger.LogWarning("Seller {UserId} attempted to update product but has no vendor profile", currentUserId);
                return Result<ProductDto>.Failure("Seller vendor profile not found", "FORBIDDEN");
            }

            if (product.VendorId != vendor.Id)
            {
                _logger.LogWarning("Seller {UserId} attempted to update product {ProductId} owned by vendor {VendorId}", currentUserId, id, product.VendorId);
                return Result<ProductDto>.Failure("You do not have permission to modify this product", "FORBIDDEN");
            }
        }

        product.Update(
            request.Name ?? product.Name,
            request.Description,
            request.Price,
            request.CompareAtPrice,
            request.LowStockThreshold,
            request.IsFeatured
        );

        await _productRepository.UpdateWithImagesAsync(product, request.ImageUrl, cancellationToken);

        _logger.LogInformation("Product updated: {ProductId}, Name: {Name}", product.Id, product.Name);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Failed to delete product: {ProductId} not found", id);
            return Result.Failure("Product not found", "PRODUCT_NOT_FOUND");
        }

        if (isSeller && !isAdmin)
        {
            if (currentUserId == null)
            {
                _logger.LogWarning("Seller attempted to delete product without user ID");
                return Result.Failure("User ID is required", "UNAUTHORIZED");
            }

            var vendor = await _vendorRepository.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            if (vendor == null)
            {
                _logger.LogWarning("Seller {UserId} attempted to delete product but has no vendor profile", currentUserId);
                return Result.Failure("Seller vendor profile not found", "FORBIDDEN");
            }

            if (product.VendorId != vendor.Id)
            {
                _logger.LogWarning("Seller {UserId} attempted to delete product {ProductId} owned by vendor {VendorId}", currentUserId, id, product.VendorId);
                return Result.Failure("You do not have permission to delete this product", "FORBIDDEN");
            }
        }

        await _productRepository.DeleteAsync(product, cancellationToken);

        _logger.LogInformation("Product deleted: {ProductId}, Name: {Name}", product.Id, product.Name);

        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
            return Result.Failure("Product not found", "PRODUCT_NOT_FOUND");

        if (product.IsActive)
            product.Deactivate();
        else
            product.Activate();

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product == null)
            return Result.Failure("Product not found", "PRODUCT_NOT_FOUND");

        if (product.IsFeatured)
            product.RemoveFromFeatured();
        else
            product.SetAsFeatured();

        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ProductDto>> AddImageAsync(Guid productId, string imageUrl, string? altText, int displayOrder, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND");

        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result<ProductDto>.Failure("Image URL is required", "INVALID_IMAGE_URL");

        product.AddImage(imageUrl, altText, displayOrder);
        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result> RemoveImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product == null)
            return Result.Failure("Product not found", "PRODUCT_NOT_FOUND");

        product.RemoveImage(imageId);
        await _productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }

    private static ProductDto MapToDto(Product product)
    {
        var images = product.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.DisplayOrder))
            .ToList();

        var mainImageUrl = product.Images
            .Where(i => i.IsActive)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault();

        return new ProductDto(
            product.Id,
            product.VendorId,
            product.CategoryId,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.CompareAtPrice,
            product.SKU,
            product.StockQuantity,
            product.LowStockThreshold,
            product.IsFeatured,
            product.IsActive,
            product.ReviewCount,
            product.AverageRating,
            product.IsInStock,
            product.IsLowStock,
            product.HasDiscount,
            product.DiscountPercentage,
            mainImageUrl,
            images,
            product.CreatedAt,
            product.UpdatedAt ?? product.CreatedAt
        );
    }

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> queryable, ProductListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLowerInvariant();
            queryable = queryable.Where(p =>
                p.Name.ToLowerInvariant().Contains(term) ||
                (p.Description != null && p.Description.ToLowerInvariant().Contains(term)));
        }

        if (query.CategoryId.HasValue)
            queryable = queryable.Where(p => p.CategoryId == query.CategoryId.Value);

        if (query.VendorId.HasValue)
            queryable = queryable.Where(p => p.VendorId == query.VendorId.Value);

        if (query.IsFeatured.HasValue)
            queryable = queryable.Where(p => p.IsFeatured == query.IsFeatured.Value);

        if (query.IsActive.HasValue)
            queryable = queryable.Where(p => p.IsActive == query.IsActive.Value);

        if (query.IsInStock.HasValue)
            queryable = queryable.Where(p => p.StockQuantity > 0 == query.IsInStock.Value);

        if (query.MinPrice.HasValue)
            queryable = queryable.Where(p => p.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            queryable = queryable.Where(p => p.Price <= query.MaxPrice.Value);

        return queryable;
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> queryable, ProductListQuery query)
    {
        return query.SortBy?.ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? queryable.OrderByDescending(p => p.Name)
                : queryable.OrderBy(p => p.Name),
            "price" => query.SortDescending
                ? queryable.OrderByDescending(p => p.Price)
                : queryable.OrderBy(p => p.Price),
            "created" => query.SortDescending
                ? queryable.OrderByDescending(p => p.CreatedAt)
                : queryable.OrderBy(p => p.CreatedAt),
            "rating" => query.SortDescending
                ? queryable.OrderByDescending(p => p.AverageRating)
                : queryable.OrderBy(p => p.AverageRating),
            _ => query.SortDescending
                ? queryable.OrderByDescending(p => p.CreatedAt)
                : queryable.OrderBy(p => p.CreatedAt)
        };
    }
}
