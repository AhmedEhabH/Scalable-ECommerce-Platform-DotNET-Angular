using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly ApplicationDbContext _context;

    public VendorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .OrderBy(v => v.BusinessName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vendor> AddAsync(Vendor entity, CancellationToken cancellationToken = default)
    {
        _context.Vendors.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Vendor entity, CancellationToken cancellationToken = default)
    {
        _context.Vendors.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Vendor entity, CancellationToken cancellationToken = default)
    {
        _context.Vendors.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vendors.AnyAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<Vendor?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Vendor>> GetApprovedVendorsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vendors
            .Where(v => v.IsApproved)
            .OrderBy(v => v.BusinessName)
            .ToListAsync(cancellationToken);
    }
}
