using CookBook.Domain.Entities;
using CookBook.Domain.Enums;
using CookBook.Domain.Services;
using CookBook.Infrastructure.Data;
using CookBook.WebApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CookBook.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DishesController(AppDbContext db)
    {
        _db = db;
    }

    // 2.1 Создание блюда
    [HttpPost]
    public async Task<ActionResult<Dish>> Create([FromBody] CreateDishRequest request)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
            return BadRequest("Название должно быть не менее 2 символов");

        if (request.PortionSize <= 0)
            return BadRequest("Размер порции должен быть больше 0");

        if (request.Composition == null || request.Composition.Count == 0)
            return BadRequest("Состав блюда должен содержать хотя бы один продукт");

        // 2.3 Обработка макросов в названии
        var (cleanedName, macroCategory) = DishNameParser.Parse(request.Name);

        // Категория: приоритет — поле формы, потом макрос
        var category = request.Category ?? macroCategory ?? DishCategory.Второе;

        // Загружаем продукты по составу
        var productIds = request.Composition.Select(c => c.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        if (products.Count != productIds.Count)
            return BadRequest("Некоторые продукты не найдены");

        // 2.2 Автоматический расчёт КБЖУ
        var composition = products.Select(p =>
        {
            var dto = request.Composition.First(c => c.ProductId == p.Id);
            return (product: p, amount: dto.Amount);
        }).ToList();

        var (calories, proteins, fats, carbs) = CaloriesCalculator.Calculate(composition);

        // 2.4 Проверка доступности флагов
        var availableFlags = GetAvailableFlags(products);

        // Сумма БЖУ на 100г порции
        var portionSize = request.PortionSize;
        var per100Proteins = proteins * 100 / portionSize;
        var per100Fats = fats * 100 / portionSize;
        var per100Carbs = carbs * 100 / portionSize;

        if (per100Proteins + per100Fats + per100Carbs > 100)
            return BadRequest("Сумма БЖУ на 100 грамм не может превышать 100");

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = cleanedName,
            Photos = request.Photos,
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs,
            PortionSize = portionSize,
            Category = category,
            Flags = DishFlag.None,
            CreatedAt = DateTime.UtcNow,
            DishProducts = composition.Select(c => new DishProduct
            {
                Id = Guid.NewGuid(),
                ProductId = c.product.Id,
                Amount = c.amount
            }).ToList()
        };

        _db.Dishes.Add(dish);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dish.Id }, dish);
    }

    // 2.5 Отображение блюд
    [HttpGet]
    public async Task<ActionResult<List<Dish>>> GetAll(
        [FromQuery] DishCategory? category = null,
        [FromQuery] bool? vegan = null,
        [FromQuery] bool? glutenFree = null,
        [FromQuery] bool? sugarFree = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Dishes
            .Include(d => d.DishProducts)
            .ThenInclude(dp => dp.Product)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(d => d.Category == category.Value);

        if (vegan == true)
            query = query.Where(d => d.Flags.HasFlag(DishFlag.Веган));
        if (glutenFree == true)
            query = query.Where(d => d.Flags.HasFlag(DishFlag.БезГлютена));
        if (sugarFree == true)
            query = query.Where(d => d.Flags.HasFlag(DishFlag.БезСахара));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

        query = query.OrderBy(d => d.Name);

        return await query.ToListAsync();
    }

    // 2.6 Просмотр блюда
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Dish>> GetById(Guid id)
    {
        var dish = await _db.Dishes
            .Include(d => d.DishProducts)
            .ThenInclude(dp => dp.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dish == null)
            return NotFound();

        return dish;
    }

    // 2.7 Редактирование блюда
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Dish>> Update(Guid id, [FromBody] UpdateDishRequest request)
    {
        var dish = await _db.Dishes
            .Include(d => d.DishProducts)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dish == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
            return BadRequest("Название должно быть не менее 2 символов");

        if (request.PortionSize <= 0)
            return BadRequest("Размер порции должен быть больше 0");

        if (request.Composition == null || request.Composition.Count == 0)
            return BadRequest("Состав блюда должен содержать хотя бы один продукт");

        // Макросы
        var (cleanedName, macroCategory) = DishNameParser.Parse(request.Name);
        var category = request.Category ?? macroCategory ?? dish.Category;

        // Загружаем продукты
        var productIds = request.Composition.Select(c => c.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        if (products.Count != productIds.Count)
            return BadRequest("Некоторые продукты не найдены");

        // Автоматический расчёт КБЖУ
        var composition = products.Select(p =>
        {
            var dto = request.Composition.First(c => c.ProductId == p.Id);
            return (product: p, amount: dto.Amount);
        }).ToList();

        var (autoCalories, autoProteins, autoFats, autoCarbs) = CaloriesCalculator.Calculate(composition);

        // Пользователь может скорректировать (если передал — используем его, иначе авто)
        var calories = request.Calories ?? autoCalories;
        var proteins = request.Proteins ?? autoProteins;
        var fats = request.Fats ?? autoFats;
        var carbs = request.Carbs ?? autoCarbs;

        // Сумма БЖУ на 100г
        var portionSize = request.PortionSize;
        var per100Proteins = proteins * 100 / portionSize;
        var per100Fats = fats * 100 / portionSize;
        var per100Carbs = carbs * 100 / portionSize;

        if (per100Proteins + per100Fats + per100Carbs > 100)
            return BadRequest("Сумма БЖУ на 100 грамм не может превышать 100");

        // Доступные флаги
        var availableFlags = GetAvailableFlags(products);

        // Если старые флаги больше не доступны — снимаем
        var newFlags = dish.Flags & availableFlags;

        dish.Name = cleanedName;
        dish.Photos = request.Photos;
        dish.Calories = calories;
        dish.Proteins = proteins;
        dish.Fats = fats;
        dish.Carbs = carbs;
        dish.PortionSize = portionSize;
        dish.Category = category;
        dish.Flags = newFlags;
        dish.UpdatedAt = DateTime.UtcNow;

        // Обновляем состав
        _db.DishProducts.RemoveRange(dish.DishProducts);
        dish.DishProducts = composition.Select(c => new DishProduct
        {
            Id = Guid.NewGuid(),
            DishId = dish.Id,
            ProductId = c.product.Id,
            Amount = c.amount
        }).ToList();

        await _db.SaveChangesAsync();

        return dish;
    }

    // 2.8 Удаление блюда
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var dish = await _db.Dishes.FindAsync(id);

        if (dish == null)
            return NotFound();

        _db.Dishes.Remove(dish);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Вспомогательный метод: вычисляет доступные флаги
    private static DishFlag GetAvailableFlags(List<Product> products)
    {
        var flags = DishFlag.Веган | DishFlag.БезГлютена | DishFlag.БезСахара;

        if (products.Any(p => !p.Flags.HasFlag(ProductFlag.Веган)))
            flags &= ~DishFlag.Веган;
        if (products.Any(p => !p.Flags.HasFlag(ProductFlag.БезГлютена)))
            flags &= ~DishFlag.БезГлютена;
        if (products.Any(p => !p.Flags.HasFlag(ProductFlag.БезСахара)))
            flags &= ~DishFlag.БезСахара;

        return flags;
    }
}