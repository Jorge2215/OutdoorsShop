using OutdoorsShop.Core.Entities;

namespace OutdoorsShop.Core.Interfaces;

public interface IOrderRepository : IRepository<SalesOrder>
{
    Task<IEnumerable<SalesOrder>> GetByCustomerIdAsync(int customerId);
    Task<SalesOrder?> GetWithDetailsAsync(int orderId);
}
