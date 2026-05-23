using OutdoorsShop.Core.Entities;

namespace OutdoorsShop.Core.Interfaces;

public interface IInventoryRepository : IRepository<ProductInventory>
{
    Task<IEnumerable<ProductInventory>> GetLowStockAsync();
    Task<ProductInventory?> GetByProductIdAsync(int productId);
}
