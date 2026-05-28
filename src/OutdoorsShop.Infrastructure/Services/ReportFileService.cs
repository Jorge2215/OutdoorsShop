using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;
using OutdoorsShop.Infrastructure.Data;
using System.Globalization;
using System.Text;

namespace OutdoorsShop.Infrastructure.Services;

public class ReportFileService : IReportFileService
{
    private const string CsvContentType = "text/csv";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly AppDbContext _dbContext;

    public ReportFileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GeneratedReportFileDto> BuildOrdersReportAsync(string format, DateTime? from, DateTime? to, string fileNamePrefix, CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("The 'from' date must be earlier than or equal to the 'to' date.");

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Details)
            .Where(order => !from.HasValue || order.OrderDate >= from.Value)
            .Where(order => !to.HasValue || order.OrderDate <= to.Value)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync(cancellationToken);

        var rows = orders.Select(order => new OrderReportRowDto
        {
            OrderID = order.OrderID,
            CustomerID = order.CustomerID,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            ItemCount = order.Details.Sum(detail => detail.Quantity),
            ShippingAddress = order.ShippingAddress
        }).ToList();

        return BuildReportFile(format, rows, fileNamePrefix, "orders report");
    }

    public async Task<GeneratedReportFileDto> BuildInventoryReportAsync(string format, string fileNamePrefix, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Inventory
            .AsNoTracking()
            .Include(item => item.Product)
            .OrderBy(item => item.ProductID)
            .ToListAsync(cancellationToken);

        var rows = items.Select(item => new InventoryReportRowDto
        {
            ProductID = item.ProductID,
            ProductName = item.Product?.Name ?? string.Empty,
            QuantityAvailable = item.QuantityAvailable,
            ReorderThreshold = item.ReorderThreshold,
            LastUpdated = item.LastUpdated,
            IsLowStock = item.QuantityAvailable <= item.ReorderThreshold
        }).ToList();

        return BuildReportFile(format, rows, fileNamePrefix, "inventory report");
    }

    private static GeneratedReportFileDto BuildReportFile<T>(string format, IReadOnlyList<T> rows, string fileNamePrefix, string reportDescription)
    {
        var normalizedFormat = format.Trim().ToLowerInvariant();
        return normalizedFormat switch
        {
            "csv" => new GeneratedReportFileDto
            {
                Content = BuildCsv(rows),
                ContentType = CsvContentType,
                FileName = $"{fileNamePrefix}.csv"
            },
            "excel" => new GeneratedReportFileDto
            {
                Content = BuildExcel(rows, fileNamePrefix),
                ContentType = ExcelContentType,
                FileName = $"{fileNamePrefix}.xlsx"
            },
            _ => throw new ArgumentException($"Supported formats for {reportDescription} are csv and excel.")
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
