using System.ComponentModel.DataAnnotations;

namespace OutdoorsShop.Core.DTOs.Reports;

public class ReportExportRequestCreateDto
{
    [Required]
    [RegularExpression("^(orders|inventory)$", ErrorMessage = "Supported report types are orders and inventory.")]
    public string ReportType { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(csv|excel)$", ErrorMessage = "Supported formats are csv and excel.")]
    public string Format { get; set; } = string.Empty;

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
