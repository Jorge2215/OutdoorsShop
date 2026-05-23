using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Inventory;
using OutdoorsShop.Core.DTOs.Reports;

namespace OutdoorsShop.Core.Interfaces;

public interface IInventoryService
{
    Task<PagedResult<InventoryDto>> GetPagedAsync(int pageNumber, int pageSize);
    Task<OperationResult<InventoryDto>> GetByProductIdAsync(int productId);
    Task<OperationResult<InventoryDto>> UpdateAsync(int productId, UpdateInventoryDto request);
    Task<IReadOnlyList<InventoryDto>> GetLowStockAsync();
    Task<IReadOnlyList<InventoryReportRowDto>> GetReportRowsAsync();
}
