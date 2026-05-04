using CookBook.Domain.Entities;
using CookBook.Domain.Enums;
using CookBook.Infrastructure.Data;
using CookBook.WebApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookBook.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // 1.1 Создание продукта
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductRequest dto)
    {
        var validationError = ValidateProduct(dto.Name, dto.Calories, dto.Proteins, dto.Fats, dto.Carbs);
        if (validationError != null)
            return BadRequest(validationError);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Photos = dto.Photos,
            Calories = dto.Calories,
            Proteins = dto.Proteins,
            Fats = dto.Fats,
            Carbs = dto.Carbs,
            Composition = dto.Composition,
            Category = dto.Category,
            CookingRequired = dto.CookingRequired,
            Flags = ParseFlags(dto.Flags),
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // 1.2 Отображение продуктов
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll(
        [FromQuery] ProductCategory? category = null,
        [FromQuery] CookingRequired? cookingRequired = null,
        [FromQuery] bool? vegan = null,
        [FromQuery] bool? glutenFree = null,
        [FromQuery] bool? sugarFree = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null)
    {
        var query = _db.Products.AsQueryable();

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        if (cookingRequired.HasValue)
            query = query.Where(p => p.CookingRequired == cookingRequired.Value);

        if (vegan == true)
            query = query.Where(p => p.Flags.HasFlag(ProductFlag.Веган));
        if (glutenFree == true)
            query = query.Where(p => p.Flags.HasFlag(ProductFlag.БезГлютена));
        if (sugarFree == true)
            query = query.Where(p => p.Flags.HasFlag(ProductFlag.БезСахара));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        query = sortBy?.ToLower() switch
        {
            "name" => query.OrderBy(p => p.Name),
            "calories" => query.OrderBy(p => p.Calories),
            "proteins" => query.OrderBy(p => p.Proteins),
            "fats" => query.OrderBy(p => p.Fats),
            "carbs" => query.OrderBy(p => p.Carbs),
            _ => query.OrderBy(p => p.Name)
        };

        return await query.ToListAsync();
    }

    // 1.3 Просмотр продукта
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return product;
    }

    // 1.4 Редактирование продукта
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Product>> Update(Guid id, [FromBody] UpdateProductRequest dto)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        var validationError = ValidateProduct(dto.Name, dto.Calories, dto.Proteins, dto.Fats, dto.Carbs);
        if (validationError != null)
            return BadRequest(validationError);

        product.Name = dto.Name;
        product.Photos = dto.Photos;
        product.Calories = dto.Calories;
        product.Proteins = dto.Proteins;
        product.Fats = dto.Fats;
        product.Carbs = dto.Carbs;
        product.Composition = dto.Composition;
        product.Category = dto.Category;
        product.CookingRequired = dto.CookingRequired;
        product.Flags = ParseFlags(dto.Flags);
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return product;
    }

    // 1.5 Удаление продукта
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
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
            return BadRequest(new
            {
                Message = "Невозможно удалить продукт, так как он используется в блюдах",
                Dishes = dishNames
            });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static ProductFlag ParseFlags(List<string>? flags)
    {
        if (flags == null || flags.Count == 0)
            return ProductFlag.None;

        ProductFlag result = ProductFlag.None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<ProductFlag>(flag, true, out var parsed))
                result |= parsed;
        }
        return result;
    }

    private static string? ValidateProduct(string name, float calories, float proteins, float fats, float carbs)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            return "Название должно быть не менее 2 символов";
        if (calories < 0)
            return "Калорийность не может быть отрицательной";
        if (proteins < 0 || proteins > 100)
            return "Белки должны быть от 0 до 100";
        if (fats < 0 || fats > 100)
            return "Жиры должны быть от 0 до 100";
        if (carbs < 0 || carbs > 100)
            return "Углеводы должны быть от 0 до 100";
        if (proteins + fats + carbs > 100)
            return "Сумма БЖУ на 100 грамм не может превышать 100";
        return null;
    }
}