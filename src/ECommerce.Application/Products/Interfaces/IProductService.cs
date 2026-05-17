using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.DTOs;

namespace ECommerce.Application.Products.Interfaces;

public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PaginatedResult<ProductDto>>> GetPagedAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ProductDto>>> GetFeaturedAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ProductDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ProductDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> CreateAsync(Guid? vendorId, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default);
    Task<Result> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ProductDto>> AddImageAsync(Guid productId, string imageUrl, string? altText, int displayOrder, Guid? currentUserId, bool isAdmin, bool isSeller, CancellationToken cancellationToken = default);
    Task<Result> RemoveImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
}
