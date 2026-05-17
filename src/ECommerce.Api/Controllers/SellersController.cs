using ECommerce.Application.Sellers.DTOs;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("global")]
public class SellersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public SellersController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SellerDto>>> GetSellers(CancellationToken cancellationToken)
    {
        var sellers = await _context.Vendors
            .Where(v => v.IsActive)
            .Select(v => new SellerDto(
                v.Id,
                v.BusinessName,
                v.Description,
                v.LogoUrl,
                v.Products.Count,
                v.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(sellers);
    }

    [HttpGet("{sellerId:guid}")]
    public async Task<ActionResult<SellerDetailDto>> GetSeller(Guid sellerId, CancellationToken cancellationToken)
    {
        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(v => v.Id == sellerId && v.IsActive, cancellationToken);

        if (vendor == null)
            return NotFound();

        var detail = new SellerDetailDto(
            vendor.Id,
            vendor.BusinessName,
            vendor.Description,
            vendor.LogoUrl,
            vendor.ContactEmail,
            vendor.ContactPhone,
            vendor.IsApproved,
            vendor.Products.Count,
            vendor.CreatedAt,
            vendor.ApprovedAt);

        return Ok(detail);
    }

    [HttpGet("{sellerId:guid}/products")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetSellerProducts(Guid sellerId, CancellationToken cancellationToken)
    {
        var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == sellerId && v.IsActive, cancellationToken);
        if (!vendorExists)
            return NotFound();

        var products = await _context.Products
            .Where(p => p.VendorId == sellerId && p.IsActive)
            .Select(p => new ProductDto(
                p.Id,
                p.VendorId,
                p.CategoryId,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.CompareAtPrice,
                p.SKU,
                p.StockQuantity,
                p.LowStockThreshold,
                p.IsFeatured,
                p.IsActive,
                p.ReviewCount,
                p.AverageRating,
                p.StockQuantity > 0,
                p.StockQuantity <= p.LowStockThreshold,
                p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price,
                0,
                p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault().ImageUrl ?? string.Empty,
                p.Images.Select(i => new ProductImageDto(i.Id, i.ImageUrl, i.AltText, i.DisplayOrder)).ToList(),
                p.CreatedAt,
                p.UpdatedAt ?? DateTime.UtcNow))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("products/{productId:guid}/seller")]
    public async Task<ActionResult<SellerDto>> GetProductSeller(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);

        if (product?.Vendor == null || !product.Vendor.IsActive)
            return NotFound();

        var seller = new SellerDto(
            product.Vendor.Id,
            product.Vendor.BusinessName,
            product.Vendor.Description,
            product.Vendor.LogoUrl,
            product.Vendor.Products.Count,
            product.Vendor.CreatedAt);

        return Ok(seller);
    }
}