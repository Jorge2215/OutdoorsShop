using OutdoorsShop.Core.Entities;

namespace OutdoorsShop.Core.Interfaces;

public interface IInventoryRepository : IRepository<ProductInventory>
{
    Task<(IReadOnlyList<ProductInventory> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<IEnumerable<ProductInventory>> GetLowStockAsync();
    Task<ProductInventory?> GetByProductIdAsync(int productId);
    Task<ProductInventory?> EnsureForProductIdAsync(int productId);
    Task<int> EnsureForAllProductsAsync();
}
