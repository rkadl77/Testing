using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBook.Core.DTOs;
using RecipeBook.Core.Interfaces;
using RecipeBook.Core.Models;
using RecipeBook.Data;

namespace RecipeBook.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICalorieCalculator _calculator;
    private readonly IValidationService _validator;

    public DishesController(AppDbContext db, ICalorieCalculator calculator, IValidationService validator)
    {
        _db = db;
        _calculator = calculator;
        _validator = validator;
    }

    // 2.1 Создание блюда
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDishDto dto)
    {
        // ✅ НОВАЯ ПРОВЕРКА ВАЛИДАЦИИ DTO
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return BadRequest(string.Join("; ", errors));
        }

        // Проверка наличия ингредиентов (дополнительная защита)
        if (dto.Ingredients == null || dto.Ingredients.Count == 0)
            return BadRequest("Блюдо должно содержать хотя бы один продукт");

        var productIds = dto.Ingredients.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        if (products.Count != productIds.Count)
            return BadRequest("Некоторые продукты не найдены");

        var dishProducts = dto.Ingredients.Select(i =>
        {
            var product = products.First(p => p.Id == i.ProductId);
            return new DishProduct
            {
                ProductId = product.Id,
                Product = product,
                Quantity = i.Quantity
            };
        }).ToList();

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Photos = dto.Photos ?? new(),
            PortionSize = dto.PortionSize,
            DishProducts = dishProducts,
            CreatedAt = DateTime.UtcNow
        };

        // Авторасчёт КБЖУ
        var (cal, prot, fats, carbs) = _calculator.Calculate(dish);
        dish.Calories = dto.Calories ?? cal;
        dish.Proteins = dto.Proteins ?? prot;
        dish.Fats = dto.Fats ?? fats;
        dish.Carbs = dto.Carbs ?? carbs;

        if (dish.Photos.Count > 5)
            return BadRequest("Нельзя добавить более 5 фото");

        // Проверка суммы БЖУ на 100г (закомментирована)
        //if (!_validator.IsBjuSumValid(dish))
        //    return BadRequest("Сумма БЖУ на 100г не может превышать 100");

        // Обработка макросов в названии
        var macroCategory = _validator.ExtractCategoryFromName(dto.Name);
        dish.Name = _validator.RemoveCategoryMacros(dto.Name).Trim();

        // Проверка минимальной длины названия после удаления макроса
        if (string.IsNullOrWhiteSpace(dish.Name) || dish.Name.Length < 2)
            return BadRequest("Название блюда должно содержать минимум 2 символа после удаления макроса");

        // Категория
        if (!string.IsNullOrEmpty(dto.Category))
        {
            try
            {
                dish.Category = Enum.Parse<DishCategory>(dto.Category.Replace(" ", ""), true);
            }
            catch
            {
                return BadRequest($"Недопустимая категория: {dto.Category}");
            }
        }
        else if (macroCategory != null)
        {
            dish.Category = Enum.Parse<DishCategory>(macroCategory);
        }
        else
        {
            return BadRequest("Не указана категория блюда");
        }

        // Флаги
        var availableFlags = _validator.GetAvailableFlags(dish);
        if (!string.IsNullOrEmpty(dto.Flags))
        {
            try
            {
                var requestedFlags = Enum.Parse<ProductFlags>(dto.Flags.Replace(" ", ""), true);
                if ((requestedFlags & ~availableFlags) != 0)
                    return BadRequest($"Некоторые флаги недоступны. Доступные: {availableFlags}");
                dish.Flags = requestedFlags;
            }
            catch
            {
                return BadRequest($"Недопустимый флаг: {dto.Flags}");
            }
        }

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dish.Id }, MapToDto(dish));
    }

    // 2.5 Отображение списка блюд
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? flags,
        [FromQuery] string? search)
    {
        var query = _db.Dishes
            .Include(d => d.DishProducts)
            .ThenInclude(dp => dp.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            try
            {
                var cat = Enum.Parse<DishCategory>(category.Replace(" ", ""), true);
                query = query.Where(d => d.Category == cat);
            }
            catch
            {
                return BadRequest($"Недопустимая категория: {category}");
            }
        }

        if (!string.IsNullOrEmpty(flags))
        {
            try
            {
                var flag = Enum.Parse<ProductFlags>(flags.Replace(" ", ""), true);
                query = query.Where(d => (d.Flags & flag) == flag);
            }
            catch
            {
                return BadRequest($"Недопустимый флаг: {flags}");
            }
        }

        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

        var dishes = await query.ToListAsync();
        return Ok(dishes.Select(MapToDto));
    }

    // 2.6 Просмотр блюда
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dish = await _db.Dishes
            .Include(d => d.DishProducts)
            .ThenInclude(dp => dp.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dish == null)
            return NotFound();

        return Ok(MapToDto(dish));
    }

    // 2.7 Редактирование блюда
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDishDto dto)
    {
        // ✅ НОВАЯ ПРОВЕРКА ВАЛИДАЦИИ DTO
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return BadRequest(string.Join("; ", errors));
        }

        // Проверка наличия ингредиентов
        if (dto.Ingredients == null || dto.Ingredients.Count == 0)
            return BadRequest("Блюдо должно содержать хотя бы один продукт");

        var dish = await _db.Dishes
            .Include(d => d.DishProducts)
            .ThenInclude(dp => dp.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dish == null)
            return NotFound();

        _db.DishProducts.RemoveRange(dish.DishProducts);

        var productIds = dto.Ingredients.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        if (products.Count != productIds.Count)
            return BadRequest("Некоторые продукты не найдены");

        dish.DishProducts = dto.Ingredients.Select(i =>
        {
            var product = products.First(p => p.Id == i.ProductId);
            return new DishProduct
            {
                DishId = dish.Id,
                ProductId = product.Id,
                Product = product,
                Quantity = i.Quantity
            };
        }).ToList();

        dish.Name = dto.Name;
        dish.Photos = dto.Photos ?? new();
        dish.PortionSize = dto.PortionSize;
        dish.UpdatedAt = DateTime.UtcNow;

        var (cal, prot, fats, carbs) = _calculator.Calculate(dish);
        dish.Calories = dto.Calories ?? cal;
        dish.Proteins = dto.Proteins ?? prot;
        dish.Fats = dto.Fats ?? fats;
        dish.Carbs = dto.Carbs ?? carbs;

        if (dish.Photos.Count > 5)
            return BadRequest("Нельзя добавить более 5 фото");
        //if (!_validator.IsBjuSumValid(dish))
        //return BadRequest("Сумма БЖУ на 100г не может превышать 100");

        var macroCategory = _validator.ExtractCategoryFromName(dto.Name);
        dish.Name = _validator.RemoveCategoryMacros(dto.Name).Trim();

        // минимальная длина названия после удаления макроса
        if (string.IsNullOrWhiteSpace(dish.Name) || dish.Name.Length < 2)
            return BadRequest("Название блюда должно содержать минимум 2 символа после удаления макроса");

        if (!string.IsNullOrEmpty(dto.Category))
        {
            try
            {
                dish.Category = Enum.Parse<DishCategory>(dto.Category.Replace(" ", ""), true);
            }
            catch
            {
                return BadRequest($"Недопустимая категория: {dto.Category}");
            }
        }
        else if (macroCategory != null)
        {
            dish.Category = Enum.Parse<DishCategory>(macroCategory);
        }

        var availableFlags = _validator.GetAvailableFlags(dish);
        if (!string.IsNullOrEmpty(dto.Flags))
        {
            try
            {
                var requestedFlags = Enum.Parse<ProductFlags>(dto.Flags.Replace(" ", ""), true);
                if ((requestedFlags & ~availableFlags) != 0)
                    return BadRequest($"Некоторые флаги недоступны. Доступные: {availableFlags}");
                dish.Flags = requestedFlags;
            }
            catch
            {
                return BadRequest($"Недопустимый флаг: {dto.Flags}");
            }
        }
        else
        {
            dish.Flags &= availableFlags;
        }

        await _db.SaveChangesAsync();
        return Ok(MapToDto(dish));
    }

    // 2.8 Удаление блюда
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var dish = await _db.Dishes.FindAsync(id);
        if (dish == null)
            return NotFound();

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private DishDto MapToDto(Dish d)
    {
        return new DishDto
        {
            Id = d.Id,
            Name = d.Name,
            Photos = d.Photos,
            Calories = d.Calories,
            Proteins = d.Proteins,
            Fats = d.Fats,
            Carbs = d.Carbs,
            PortionSize = d.PortionSize,
            Category = d.Category.ToString(),
            Flags = d.Flags.ToString(),
            Ingredients = d.DishProducts?.Select(dp => new DishIngredientDto
            {
                ProductId = dp.ProductId,
                ProductName = dp.Product?.Name ?? "",
                Quantity = dp.Quantity
            }).ToList() ?? new(),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}