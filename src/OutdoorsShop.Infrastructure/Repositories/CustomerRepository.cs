using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public override async Task<Customer?> GetByIdAsync(int id)
        => await _dbSet.FirstOrDefaultAsync(c => c.CustomerID == id);

    public async Task<Customer?> GetByUserIdAsync(string userId)
        => await _dbSet.FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var query = _dbSet.OrderBy(c => c.CustomerID);
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
