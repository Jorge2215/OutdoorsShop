using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Interfaces;
using System.Globalization;
using System.Text;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Administrator")]
public class ReportsController : ControllerBase
{
    private const string CsvContentType = "text/csv";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly IOrderService _orderService;
    private readonly IInventoryService _inventoryService;

    public ReportsController(IOrderService orderService, IInventoryService inventoryService)
    {
        _orderService = orderService;
        _inventoryService = inventoryService;
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OrdersReport([FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { message = "The 'from' date must be earlier than or equal to the 'to' date." });

        var rows = await _orderService.GetReportRowsAsync(from, to);
        return CreateReportResult(format, rows, "orders-report");
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InventoryReport([FromQuery] string format = "csv")
    {
        var rows = await _inventoryService.GetReportRowsAsync();
        return CreateReportResult(format, rows, "inventory-report");
    }

    private IActionResult CreateReportResult<T>(string format, IReadOnlyList<T> rows, string fileNamePrefix)
    {
        var normalizedFormat = format.Trim().ToLowerInvariant();
        return normalizedFormat switch
        {
            "csv" => File(BuildCsv(rows), CsvContentType, $"{fileNamePrefix}.csv"),
            "excel" => File(BuildExcel(rows, fileNamePrefix), ExcelContentType, $"{fileNamePrefix}.xlsx"),
            _ => BadRequest(new { message = "Supported formats are csv and excel." })
        };
    }

    private static byte[] BuildCsv<T>(IReadOnlyList<T> rows)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(stringWriter, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteHeader<T>();
        csv.NextRecord();
        csv.WriteRecords(rows);
        return Encoding.UTF8.GetBytes(stringWriter.ToString());
    }

    private static byte[] BuildExcel<T>(IReadOnlyList<T> rows, string worksheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(NormalizeWorksheetName(worksheetName));

        if (rows.Count > 0)
        {
            worksheet.Cell(1, 1).InsertTable(rows);
        }
        else
        {
            var properties = typeof(T).GetProperties();
            for (var index = 0; index < properties.Length; index++)
                worksheet.Cell(1, index + 1).Value = properties[index].Name;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string NormalizeWorksheetName(string value)
    {
        var sanitized = value.Replace('-', ' ');
        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}
