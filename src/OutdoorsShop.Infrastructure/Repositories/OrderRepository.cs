using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;

namespace OutdoorsShop.Infrastructure.Repositories;

public class OrderRepository : Repository<SalesOrder>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public override async Task<SalesOrder?> GetByIdAsync(int id)
        => await _dbSet.FirstOrDefaultAsync(o => o.OrderID == id);

    public async Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status, int? customerId = null)
    {
        var query = BuildQuery();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerID == customerId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<SalesOrder>> GetByCustomerIdAsync(int customerId)
        => await BuildQuery()
            .Where(o => o.CustomerID == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<SalesOrder?> GetWithDetailsAsync(int orderId)
        => await BuildQuery().FirstOrDefaultAsync(o => o.OrderID == orderId);

    public async Task<IReadOnlyList<SalesOrder>> GetForReportAsync(DateTime? from, DateTime? to)
    {
        var query = BuildQuery();

        if (from.HasValue)
            query = query.Where(o => o.OrderDate >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.OrderDate <= to.Value);

        return await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    private IQueryable<SalesOrder> BuildQuery()
        => _dbSet
            .Include(o => o.Customer)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .AsQueryable();
}
