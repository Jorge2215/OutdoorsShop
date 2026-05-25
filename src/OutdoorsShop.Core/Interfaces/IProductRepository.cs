using OutdoorsShop.Core.Entities;

namespace OutdoorsShop.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> SearchAsync(string term);
    Task<IEnumerable<Product>> GetActiveAsync();
    Task<IEnumerable<Product>> GetAllIncludingInactiveAsync();
    Task<Product?> GetByIdIncludingInactiveAsync(int id);
}
