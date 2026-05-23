using OutdoorsShop.Core.Entities;

namespace OutdoorsShop.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(string userId);
}
