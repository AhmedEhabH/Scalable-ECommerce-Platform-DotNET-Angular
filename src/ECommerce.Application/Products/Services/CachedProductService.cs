using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Products.Interfaces;

namespace ECommerce.Application.Products.Services;

public class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly ICacheService _cache;

    public CachedProductService(IProductService inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";
        var cached = await _cache.GetAsync<Result<ProductDto>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);

        return result;
    }

    public async Task<Result<PaginatedResult<ProductDto>>> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:page:{query.Page}:{query.PageSize}:{BuildFilterHash(query)}";
        var cached = await _cache.GetAsync<Result<PaginatedResult<ProductDto>>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.GetPagedAsync(query, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }

    public async Task<Result<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:slug:{slug}";
        var cached = await _cache.GetAsync<Result<ProductDetailResponse>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.GetBySlugAsync(slug, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), cancellationToken);

        return result;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetFeaturedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:featured:{count}";
        var cached = await _cache.GetAsync<Result<IReadOnlyList<ProductDto>>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.GetFeaturedAsync(count, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), cancellationToken);

        return result;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:category:{categoryId}";
        var cached = await _cache.GetAsync<Result<IReadOnlyList<ProductDto>>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.GetByCategoryAsync(categoryId, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), cancellationToken);

        return result;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:search:{searchTerm.ToLowerInvariant()}";
        var cached = await _cache.GetAsync<Result<IReadOnlyList<ProductDto>>>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var result = await _inner.SearchAsync(searchTerm, cancellationToken);
        if (result.IsSuccess)
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }

    public async Task<Result<ProductDto>> CreateAsync(Guid? vendorId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateAsync(vendorId, request, cancellationToken);
        if (result.IsSuccess)
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        return result;
    }

    public async Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(id, request, currentUserId, isAdmin, isSeller, cancellationToken);
        if (result.IsSuccess)
        {
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        }
        return result;
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteAsync(id, currentUserId, isAdmin, isSeller, cancellationToken);
        if (result.IsSuccess)
        {
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        }
        return result;
    }

    public async Task<Result> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ToggleActiveAsync(id, cancellationToken);
        if (result.IsSuccess)
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        return result;
    }

    public async Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.ToggleFeaturedAsync(id, cancellationToken);
        if (result.IsSuccess)
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        return result;
    }

    public async Task<Result<ProductDto>> AddImageAsync(Guid productId, string imageUrl, string? altText, int displayOrder, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default)
    {
        var result = await _inner.AddImageAsync(productId, imageUrl, altText, displayOrder, currentUserId, isAdmin, isSeller, cancellationToken);
        if (result.IsSuccess)
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        return result;
    }

    public async Task<Result> RemoveImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var result = await _inner.RemoveImageAsync(productId, imageId, cancellationToken);
        if (result.IsSuccess)
            await _cache.RemoveByPrefixAsync("product:", cancellationToken);
        return result;
    }

    private static string BuildFilterHash(ProductListQuery query)
    {
        return $"{query.CategoryId}-{query.VendorId}-{query.IsFeatured}-{query.IsActive}-{query.IsInStock}-{query.MinPrice}-{query.MaxPrice}-{query.SortBy}-{query.SortDescending}";
    }
}
