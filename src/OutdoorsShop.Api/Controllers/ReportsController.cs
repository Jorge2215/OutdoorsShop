using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Reports;
using OutdoorsShop.Core.Interfaces;
using System.Security.Claims;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Administrator")]
public class ReportsController : ControllerBase
{
    private readonly IReportFileService _reportFileService;
    private readonly IReportExportRequestService _reportExportRequestService;

    public ReportsController(IReportFileService reportFileService, IReportExportRequestService reportExportRequestService)
    {
        _reportFileService = reportFileService;
        _reportExportRequestService = reportExportRequestService;
    }

    [HttpGet("orders")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OrdersReport([FromQuery] string format = "csv", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { message = "The 'from' date must be earlier than or equal to the 'to' date." });

        try
        {
            var report = await _reportFileService.BuildOrdersReportAsync(format, from, to, "orders-report");
            return File(report.Content, report.ContentType, report.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InventoryReport([FromQuery] string format = "csv")
    {
        try
        {
            var report = await _reportFileService.BuildInventoryReportAsync(format, "inventory-report");
            return File(report.Content, report.ContentType, report.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("requests")]
    [ProducesResponseType(typeof(ReportExportRequestDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequest([FromBody] ReportExportRequestCreateDto request)
    {
        var result = await _reportExportRequestService.CreateAsync(request, GetCurrentUserId());
        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Report export request could not be created." });

        return AcceptedAtAction(nameof(GetRequestById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(typeof(ReportExportRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequestById(Guid id)
    {
        var result = await _reportExportRequestService.GetByIdAsync(id);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Report export request could not be retrieved." });

        return Ok(result.Value);
    }

    [HttpGet("requests/{id:guid}/download")]
    [ProducesResponseType(typeof(ReportExportDownloadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _reportExportRequestService.GetDownloadAsync(id);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return Conflict(new { message = result.ErrorMessage ?? "Report export is not ready for download." });

        return Ok(result.Value);
    }

    private string? GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
