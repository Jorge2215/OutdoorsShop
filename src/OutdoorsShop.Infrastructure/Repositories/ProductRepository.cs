using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await _dbSet.Where(p => p.CategoryID == categoryId).ToListAsync();

    public async Task<IEnumerable<Product>> SearchAsync(string term)
        => await _dbSet
            .Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)))
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetActiveAsync()
        => await _dbSet.ToListAsync(); // global query filter already applies IsActive = true
}
