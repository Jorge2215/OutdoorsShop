using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.Interfaces;

public interface IOrderRepository : IRepository<SalesOrder>
{
    Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status, int? customerId = null);
    Task<IEnumerable<SalesOrder>> GetByCustomerIdAsync(int customerId);
    Task<SalesOrder?> GetWithDetailsAsync(int orderId);
    Task<IReadOnlyList<SalesOrder>> GetForReportAsync(DateTime? from, DateTime? to);
}
