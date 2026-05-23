using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class InventoryRepository : Repository<ProductInventory>, IInventoryRepository
{
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
}
