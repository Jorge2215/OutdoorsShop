using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private const string DefaultSort = "name_asc";
    private const string PriceAscendingSort = "price_asc";
    private const string PriceDescendingSort = "price_desc";

    public ProductRepository(AppDbContext context) : base(context) { }

    public override async Task<Product?> GetByIdAsync(int id)
        => await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductID == id);

    public override async Task<IEnumerable<Product>> GetAllAsync()
        => await SearchProductsAsync(search: null, categoryId: null, minPrice: null, maxPrice: null, sort: null);

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
        => await SearchProductsAsync(search: null, categoryId, minPrice: null, maxPrice: null, sort: null);

    public async Task<IEnumerable<Product>> SearchAsync(string term)
        => await SearchProductsAsync(term, categoryId: null, minPrice: null, maxPrice: null, sort: null);

    public async Task<IEnumerable<Product>> SearchProductsAsync(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sort)
    {
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            return [];

        IQueryable<Product> query = _dbSet
            .Include(p => p.Category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryID == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        query = NormalizeSort(sort) switch
        {
            PriceAscendingSort => query.OrderBy(p => p.Price).ThenBy(p => p.Name),
            PriceDescendingSort => query.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveAsync()
        => await SearchProductsAsync(search: null, categoryId: null, minPrice: null, maxPrice: null, sort: null); // global query filter already applies IsActive = true

    private static string NormalizeSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return DefaultSort;

        return sort.Trim().ToLowerInvariant() switch
        {
            PriceAscendingSort => PriceAscendingSort,
            PriceDescendingSort => PriceDescendingSort,
            DefaultSort => DefaultSort,
            _ => DefaultSort
        };
    }
}
