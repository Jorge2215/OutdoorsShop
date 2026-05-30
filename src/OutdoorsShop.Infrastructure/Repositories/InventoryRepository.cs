using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class InventoryRepository : Repository<ProductInventory>, IInventoryRepository
{
    private const int DefaultReorderThreshold = 5;

    public InventoryRepository(AppDbContext context) : base(context) { }

    public override async Task<IEnumerable<ProductInventory>> GetAllAsync()
        => await _dbSet
            .Include(i => i.Product)
            .OrderBy(i => i.ProductID)
            .ToListAsync();

    public async Task<(IReadOnlyList<ProductInventory> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var query = _dbSet
            .Include(i => i.Product)
            .OrderBy(i => i.ProductID);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<ProductInventory>> GetLowStockAsync()
        => await _dbSet
            .Where(i => i.QuantityAvailable <= i.ReorderThreshold)
            .Include(i => i.Product)
            .OrderBy(i => i.ProductID)
            .ToListAsync();

    public async Task<ProductInventory?> GetByProductIdAsync(int productId)
        => await _dbSet
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductID == productId);

    public async Task<ProductInventory?> EnsureForProductIdAsync(int productId)
    {
        var existingInventory = await GetByProductIdAsync(productId);
        if (existingInventory is not null)
            return existingInventory;

        var productExists = await _context.Products
            .IgnoreQueryFilters()
            .AnyAsync(product => product.ProductID == productId);

        if (!productExists)
            return null;

        await CreateMissingInventoryAsync([productId]);
        return await GetByProductIdAsync(productId);
    }

    public async Task<int> EnsureForAllProductsAsync()
    {
        var missingProductIds = await _context.Products
            .IgnoreQueryFilters()
            .Where(product => !_context.Inventory.Any(inventory => inventory.ProductID == product.ProductID))
            .Select(product => product.ProductID)
            .ToListAsync();

        if (missingProductIds.Count == 0)
            return 0;

        await CreateMissingInventoryAsync(missingProductIds);
        return missingProductIds.Count;
    }

    private async Task CreateMissingInventoryAsync(IEnumerable<int> productIds)
    {
        var createdAt = DateTime.UtcNow;

        foreach (var productId in productIds.Distinct())
        {
            await _dbSet.AddAsync(new ProductInventory
            {
                ProductID = productId,
                QuantityAvailable = 0,
                ReorderThreshold = DefaultReorderThreshold,
                LastUpdated = createdAt
            });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            foreach (var entry in _context.ChangeTracker.Entries<ProductInventory>()
                         .Where(entry => entry.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            var stillMissing = await _context.Products
                .IgnoreQueryFilters()
                .Where(product => productIds.Contains(product.ProductID) &&
                                  !_context.Inventory.Any(inventory => inventory.ProductID == product.ProductID))
                .Select(product => product.ProductID)
                .ToListAsync();

            if (stillMissing.Count > 0)
                throw;
        }
    }
}
