using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class OrderRepository : Repository<SalesOrder>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<SalesOrder>> GetByCustomerIdAsync(int customerId)
        => await _dbSet
            .Where(o => o.CustomerID == customerId)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<SalesOrder?> GetWithDetailsAsync(int orderId)
        => await _dbSet
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderID == orderId);
}
