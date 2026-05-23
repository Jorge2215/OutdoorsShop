using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class InventoryRepository : Repository<ProductInventory>, IInventoryRepository
{
    public InventoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ProductInventory>> GetLowStockAsync()
        => await _dbSet
            .Where(i => i.QuantityAvailable <= i.ReorderThreshold)
            .Include(i => i.Product)
            .ToListAsync();

    public async Task<ProductInventory?> GetByProductIdAsync(int productId)
        => await _dbSet
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductID == productId);
}
