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
    private readonly ICategoryRepository _categoryRepo;

    public CategoriesController(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    // GET /api/v1/categories
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryRepo.GetAllAsync();
        return Ok(categories.Select(ToDto));
    }

    // GET /api/v1/categories/{id}
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null)
            return NotFound(new { message = $"Category {id} not found." });

        return Ok(ToDto(category));
    }

    // POST /api/v1/categories  [Administrator]
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var category = new ProductCategory
        {
            Name = dto.Name,
            IsActive = true
        };

        await _categoryRepo.AddAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryID }, ToDto(category));
    }

    // PUT /api/v1/categories/{id}  [Administrator]
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null)
            return NotFound(new { message = $"Category {id} not found." });

        category.Name = dto.Name;
        category.IsActive = dto.IsActive;

        await _categoryRepo.UpdateAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    // DELETE /api/v1/categories/{id}  [Administrator] — soft delete
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category is null)
            return NotFound(new { message = $"Category {id} not found." });

        category.IsActive = false;
        await _categoryRepo.UpdateAsync(category);
        await _categoryRepo.SaveChangesAsync();

        return NoContent();
    }

    private static CategoryDto ToDto(ProductCategory c) => new()
    {
        CategoryID = c.CategoryID,
        Name = c.Name,
        IsActive = c.IsActive
    };
}
