using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public override async Task<Product?> GetByIdAsync(int id)
        => await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductID == id);

    public override async Task<IEnumerable<Product>> GetAllAsync()
        => await _dbSet
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await _dbSet
            .Include(p => p.Category)
            .Where(p => p.CategoryID == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<IEnumerable<Product>> SearchAsync(string term)
        => await _dbSet
            .Include(p => p.Category)
            .Where(p => p.Name.Contains(term) ||
                        (p.Description != null && p.Description.Contains(term)))
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetActiveAsync()
        => await _dbSet
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync(); // global query filter already applies IsActive = true
}
