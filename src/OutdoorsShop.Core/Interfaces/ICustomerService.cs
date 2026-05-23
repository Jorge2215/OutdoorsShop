using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Customers;

namespace OutdoorsShop.Core.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(int pageNumber, int pageSize);
    Task<OperationResult<CustomerDto>> GetByIdAsync(int id, bool isAdministrator, int? currentCustomerId);
    Task<OperationResult<CustomerDto>> UpdateAsync(int id, UpdateCustomerDto request, bool isAdministrator, int? currentCustomerId);
    Task<OperationResult> SoftDeleteAsync(int id);
}
