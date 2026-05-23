using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutdoorsShop.Core.DTOs.Products;
using OutdoorsShop.Core.Entities;
using OutdoorsShop.Core.Interfaces;

namespace OutdoorsShop.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoriesController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    /// <summary>Get all active categories.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var dtos = categories.Select(c => new { c.CategoryID, c.Name, c.IsActive });
        return Ok(dtos);
    }

    /// <summary>Get a category by ID.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
            return NotFound();

        return Ok(new { category.CategoryID, category.Name, category.IsActive });
    }
}
