using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Orders;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Enums;

namespace OutdoorsShop.Core.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetPagedAsync(int pageNumber, int pageSize, OrderStatus? status, bool isAdministrator, int? currentCustomerId);
    Task<OperationResult<OrderDto>> GetByIdAsync(int id, bool isAdministrator, int? currentCustomerId);
    Task<OperationResult<OrderReceiptDto>> GetReceiptAsync(int id, bool isAdministrator, int? currentCustomerId);
    Task<OperationResult<OrderDto>> CreateAsync(int currentCustomerId, CreateOrderRequest request);
    Task<OperationResult<OrderDto>> UpdateStatusAsync(int id, UpdateOrderStatusDto request);
    Task<OperationResult> CancelAsync(int id);
    Task<IReadOnlyList<OrderReportRowDto>> GetReportRowsAsync(DateTime? from, DateTime? to);
}
