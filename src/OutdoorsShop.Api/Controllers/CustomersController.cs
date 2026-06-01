using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OutdoorsShop.Core.DTOs.Common;
using OutdoorsShop.Core.DTOs.Customers;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class CustomersController : ControllerBase
{
    private static readonly HashSet<string> AllowedAvatarContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    private const long MaxAvatarFileSizeBytes = 2 * 1024 * 1024;
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _customerService.GetPagedAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _customerService.GetByIdAsync(id, User.IsInRole("Administrator"), GetCurrentCustomerId());
        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto request)
    {
        var result = await _customerService.UpdateAsync(id, request, User.IsInRole("Administrator"), GetCurrentCustomerId());
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No avatar file uploaded." });

        if (!AllowedAvatarContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Invalid avatar file type. Allowed types: jpg, jpeg, png, gif, webp." });

        if (file.Length > MaxAvatarFileSizeBytes)
            return BadRequest(new { message = "Avatar file size exceeds the 2 MB limit." });

        using var stream = file.OpenReadStream();
        var result = await _customerService.UploadAvatarAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            User.IsInRole("Administrator"),
            GetCurrentCustomerId());

        return ToActionResult(result);
    }

    [HttpDelete("{id:int}/avatar")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAvatar(int id)
    {
        var result = await _customerService.RemoveAvatarAsync(id, User.IsInRole("Administrator"), GetCurrentCustomerId());
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.SoftDeleteAsync(id);
        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        return NoContent();
    }

    private int? GetCurrentCustomerId()
        => int.TryParse(User.FindFirstValue("customer_id"), out var customerId) ? customerId : null;

    private IActionResult ToActionResult(OperationResult<CustomerDto> result)
    {
        if (result.Forbidden)
            return Forbid();

        if (result.NotFound)
            return NotFound(new { message = result.ErrorMessage });

        if (!result.Succeeded || result.Value is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Request could not be completed." });

        return Ok(result.Value);
    }
}
