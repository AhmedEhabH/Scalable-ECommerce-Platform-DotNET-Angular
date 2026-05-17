using ECommerce.Application.Categories.DTOs;
using ECommerce.Application.Categories.Interfaces;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Categories.Services;

public class CachedCategoryService : ICategoryService
{
    private readonly ICategoryService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedCategoryService> _logger;

    public CachedCategoryService(ICategoryService inner, ICacheService cache, ILogger<CachedCategoryService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category:{id}";
        try
        {
            var cached = await _cache.GetAsync<Result<CategoryDto>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {Key}, falling back to database", cacheKey);
        }

        var result = await _inner.GetByIdAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key: {Key}", cacheKey);
            }
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "category:all";
        try
        {
            var cached = await _cache.GetAsync<Result<IReadOnlyList<CategoryDto>>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {Key}, falling back to database", cacheKey);
        }

        var result = await _inner.GetAllAsync(cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key: {Key}", cacheKey);
            }
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "category:root";
        try
        {
            var cached = await _cache.GetAsync<Result<IReadOnlyList<CategoryDto>>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {Key}, falling back to database", cacheKey);
        }

        var result = await _inner.GetRootCategoriesAsync(cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key: {Key}", cacheKey);
            }
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetSubcategoriesAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category:subcategories:{parentId}";
        try
        {
            var cached = await _cache.GetAsync<Result<IReadOnlyList<CategoryDto>>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {Key}, falling back to database", cacheKey);
        }

        var result = await _inner.GetSubcategoriesAsync(parentId, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key: {Key}", cacheKey);
            }
        }

        return result;
    }

    public async Task<Result<CategoryDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category:slug:{slug}";
        try
        {
            var cached = await _cache.GetAsync<Result<CategoryDto>>(cacheKey, cancellationToken);
            if (cached != null)
                return cached;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {Key}, falling back to database", cacheKey);
        }

        var result = await _inner.GetBySlugAsync(slug, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to cache for key: {Key}", cacheKey);
            }
        }

        return result;
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.RemoveByPrefixAsync("category:", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after category creation");
            }
        }
        return result;
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateAsync(id, request, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.RemoveByPrefixAsync("category:", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after category update");
            }
        }
        return result;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.RemoveByPrefixAsync("category:", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after category deletion");
            }
        }
        return result;
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeactivateAsync(id, cancellationToken);
        if (result.IsSuccess)
        {
            try
            {
                await _cache.RemoveByPrefixAsync("category:", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after category deactivation");
            }
        }
        return result;
    }
}
