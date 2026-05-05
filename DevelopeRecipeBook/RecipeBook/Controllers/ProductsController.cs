using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Core.DTOs;
using RecipeBook.Core.Interfaces;
using RecipeBook.Core.Models;
using RecipeBook.Data;

namespace RecipeBook.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IValidationService _validator;

    public ProductsController(AppDbContext db, IValidationService validator)
    {
        _db = db;
        _validator = validator;
    }

    // 1.1 Создание продукта
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Photos = dto.Photos ?? new(),
            Calories = dto.Calories,
            Proteins = dto.Proteins,
            Fats = dto.Fats,
            Carbs = dto.Carbs,
            Composition = dto.Composition,
            Category = Enum.Parse<ProductCategory>(dto.Category),
            CookingRequirement = Enum.Parse<CookingRequirement>(dto.CookingRequirement),
            Flags = Enum.Parse<ProductFlags>(dto.Flags),
            CreatedAt = DateTime.UtcNow
        };

        if (!_validator.IsBjuSumValid(product))
            return BadRequest("Сумма БЖУ на 100г не может превышать 100");

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToDto(product));
    }

    // 1.2 Отображение списка продуктов
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? cookingRequirement,
        [FromQuery] string? flags,
        [FromQuery] string? search,
        [FromQuery] string? sortBy)
    {
        var query = _db.Products.AsQueryable();

        // Фильтрация по категории
        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == Enum.Parse<ProductCategory>(category));

        // Фильтрация по необходимости готовки
        if (!string.IsNullOrEmpty(cookingRequirement))
            query = query.Where(p => p.CookingRequirement == Enum.Parse<CookingRequirement>(cookingRequirement));

        // Фильтрация по флагам
        if (!string.IsNullOrEmpty(flags))
        {
            var flag = Enum.Parse<ProductFlags>(flags);
            query = query.Where(p => p.Flags.HasFlag(flag));
        }

        // Поиск по названию
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        // Сортировка
        query = sortBy?.ToLower() switch
        {
            "calories" => query.OrderBy(p => p.Calories),
            "proteins" => query.OrderBy(p => p.Proteins),
            "fats" => query.OrderBy(p => p.Fats),
            "carbs" => query.OrderBy(p => p.Carbs),
            _ => query.OrderBy(p => p.Name)
        };

        var products = await query.ToListAsync();
        return Ok(products.Select(MapToDto));
    }

    // 1.3 Просмотр продукта
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _db.Products
            .Include(p => p.DishProducts)
            .ThenInclude(dp => dp.Dish)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        return Ok(MapToDto(product));
    }

    // 1.4 Редактирование продукта
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.Name = dto.Name;
        product.Photos = dto.Photos ?? new();
        product.Calories = dto.Calories;
        product.Proteins = dto.Proteins;
        product.Fats = dto.Fats;
        product.Carbs = dto.Carbs;
        product.Composition = dto.Composition;
        product.Category = Enum.Parse<ProductCategory>(dto.Category);
        product.CookingRequirement = Enum.Parse<CookingRequirement>(dto.CookingRequirement);
        product.Flags = Enum.Parse<ProductFlags>(dto.Flags);
        product.UpdatedAt = DateTime.UtcNow;

        if (!_validator.IsBjuSumValid(product))
            return BadRequest("Сумма БЖУ на 100г не может превышать 100");

        await _db.SaveChangesAsync();
        return Ok(MapToDto(product));
    }

    // 1.5 Удаление продукта
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _db.Products
            .Include(p => p.DishProducts)
            .ThenInclude(dp => dp.Dish)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        if (product.DishProducts.Any())
        {
            var dishNames = product.DishProducts.Select(dp => dp.Dish.Name).ToList();
            return BadRequest($"Нельзя удалить продукт. Он используется в блюдах: {string.Join(", ", dishNames)}");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private ProductDto MapToDto(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Photos = p.Photos,
            Calories = p.Calories,
            Proteins = p.Proteins,
            Fats = p.Fats,
            Carbs = p.Carbs,
            Composition = p.Composition,
            Category = p.Category.ToString(),
            CookingRequirement = p.CookingRequirement.ToString(),
            Flags = p.Flags.ToString(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            UsedInDishes = p.DishProducts?.Select(dp => dp.Dish.Name).ToList() ?? new()
        };
    }
}