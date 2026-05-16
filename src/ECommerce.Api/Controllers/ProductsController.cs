using ECommerce.Api.Models;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.DTOs;
using ECommerce.Application.Products.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Product management endpoints for browsing and managing products
/// </summary>
[Authorize]
[EnableRateLimiting("products")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public ProductsController(
        IProductService productService, 
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _productService = productService;
        _currentUserService = currentUserService;
        _context = context;
    }

    /// <summary>
    /// Get a paginated list of products with optional filtering
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/products?page=1&amp;pageSize=10&amp;searchTerm=laptop&amp;minPrice=100&amp;maxPrice=1000
    /// </remarks>
    /// <param name="query">Filtering and pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    /// <response code="200">Returns the paginated product list</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetList([FromQuery] ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = await _productService.GetPagedAsync(query, cancellationToken);
        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Bad request");
        return HandleSuccess(result.Value);
    }

    /// <summary>
    /// Get a specific product by its ID
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details</returns>
    /// <response code="200">Returns the product</response>
    /// <response code="404">Product not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return HandleNotFound(result.Error ?? "Resource not found");
        return HandleSuccess(result.Value);
    }

    /// <summary>
    /// Get the current authenticated seller's products
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Seller")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetMyProducts(CancellationToken cancellationToken)
    {
        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(v => v.UserId == _currentUserService.UserId, cancellationToken);

        if (vendor == null)
            return HandleBadRequest("Seller vendor profile not found. Please contact admin.");

        var query = new ProductListQuery(VendorId: vendor.Id, PageSize: 1000);
        var result = await _productService.GetPagedAsync(query, cancellationToken);

        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Failed to load products");

        return HandleSuccess(result.Value);
    }

    [HttpPost("by-ids")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), 200)]
    public async Task<IActionResult> GetByIds([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids == null || !ids.Any())
            return HandleSuccess(new List<ProductDto>());

        var products = new List<ProductDto>();
        foreach (var id in ids)
        {
            var result = await _productService.GetByIdAsync(id, cancellationToken);
            if (result.IsSuccess)
            {
                products.Add(result.Value);
            }
        }
        return HandleSuccess(products);
    }

    /// <summary>
    /// Create a new product (Admin or Seller)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/products
    ///     {
    ///         "categoryId": "00000000-0000-0000-0000-000000000001",
    ///         "name": "Gaming Laptop",
    ///         "slug": "gaming-laptop",
    ///         "price": 999.99,
    ///         "sku": "GL-001",
    ///         "stockQuantity": 50,
    ///         "description": "High-performance gaming laptop",
    ///         "isFeatured": true
    ///     }
    /// </remarks>
    /// <param name="request">Product creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created product</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">Insufficient permissions</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        Guid? vendorId = null;
        
        if (_currentUserService.IsSeller && _currentUserService.UserId != null)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.UserId == _currentUserService.UserId, cancellationToken);
            
            if (vendor == null)
                return HandleBadRequest("Seller vendor profile not found. Please contact admin.");
            
            vendorId = vendor.Id;
        }

        var result = await _productService.CreateAsync(vendorId, request, cancellationToken);
        if (result.IsFailure)
            return HandleBadRequest(result.Error ?? "Bad request");
        return HandleCreated(result.Value);
    }

    /// <summary>
    /// Update an existing product (Admin only)
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/products/{id}
    ///     {
    ///         "name": "Updated Gaming Laptop",
    ///         "price": 899.99
    ///     }
    /// </remarks>
    /// <param name="id">Product unique identifier</param>
    /// <param name="request">Fields to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">Product not found</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Seller")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(
            id, 
            request, 
            _currentUserService.UserId,
            _currentUserService.IsAdmin,
            _currentUserService.IsSeller,
            cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.ErrorCode == "FORBIDDEN")
                return HandleForbidden(result.Error ?? "You do not have permission to modify this product");
            return HandleNotFound(result.Error ?? "Resource not found");
        }
        return HandleSuccess(result.Value);
    }

    /// <summary>
    /// Delete a product by its ID (Admin only)
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">Product not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Seller")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(
            id,
            _currentUserService.UserId,
            _currentUserService.IsAdmin,
            _currentUserService.IsSeller,
            cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.ErrorCode == "FORBIDDEN")
                return HandleForbidden(result.Error ?? "You do not have permission to delete this product");
            return HandleNotFound(result.Error ?? "Resource not found");
        }
        return HandleOkWithMessage("Product deleted successfully");
    }
}
