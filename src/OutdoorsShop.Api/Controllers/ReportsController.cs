using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.Interfaces;
using System.Text;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Administrator")]
[Produces("text/csv")]
public class ReportsController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public ReportsController(IOrderRepository orderRepository, IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
    }

    /// <summary>Export orders report as CSV (Admin only).</summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> OrdersReport()
    {
        var orders = await _orderRepository.GetAllAsync();
        var sb = new StringBuilder();
        sb.AppendLine("OrderID,CustomerID,OrderDate,TotalAmount,Status,PaymentStatus");

        foreach (var order in orders)
            sb.AppendLine($"{order.OrderID},{order.CustomerID},{order.OrderDate:yyyy-MM-dd},{order.TotalAmount},{order.Status},{order.PaymentStatus}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"orders_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>Export inventory report as CSV (Admin only).</summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> InventoryReport()
    {
        var items = await _inventoryRepository.GetAllAsync();
        var sb = new StringBuilder();
        sb.AppendLine("ProductID,ProductName,QuantityAvailable,ReorderThreshold,LastUpdated");

        foreach (var item in items)
            sb.AppendLine($"{item.ProductID},{item.Product?.Name},{item.QuantityAvailable},{item.ReorderThreshold},{item.LastUpdated:yyyy-MM-dd}");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"inventory_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
