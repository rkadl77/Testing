using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Builders;

/// <summary>
/// Builder для создания тестовых продуктов.
/// Используется для генерации данных по эквивалентному разбиению.
/// </summary>
public class ProductTestDataBuilder
{
    private string _name = "Тестовый продукт";
    private double _calories = 100;
    private double _proteins = 10;
    private double _fats = 5;
    private double _carbs = 20;
    private string _category = "Овощи";
    private string _cookingRequirement = "Готовый_к_употреблению";
    private string _flags = "None";

    public ProductTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductTestDataBuilder WithCalories(double calories)
    {
        _calories = calories;
        return this;
    }

    public ProductTestDataBuilder WithBju(double proteins, double fats, double carbs)
    {
        _proteins = proteins;
        _fats = fats;
        _carbs = carbs;
        return this;
    }

    public ProductTestDataBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public CreateProductDto Build()
    {
        return new CreateProductDto
        {
            Name = _name,
            Calories = _calories,
            Proteins = _proteins,
            Fats = _fats,
            Carbs = _carbs,
            Category = _category,
            CookingRequirement = _cookingRequirement,
            Flags = _flags
        };
    }
}

/// <summary>
/// Builder для создания тестовых блюд.
/// </summary>
public class DishTestDataBuilder
{
    private string _name = "Тестовое блюдо";
    private double _portionSize = 500;
    private string _category = "Суп";
    private string _flags = "None";
    private List<DishProductDto> _ingredients = new();

    public DishTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public DishTestDataBuilder WithPortionSize(double portionSize)
    {
        _portionSize = portionSize;
        return this;
    }

    public DishTestDataBuilder WithCategory(string category)
    {
        _category = category;
        return this;
    }

    public DishTestDataBuilder WithIngredient(Guid productId, double quantity)
    {
        _ingredients.Add(new DishProductDto { ProductId = productId, Quantity = quantity });
        return this;
    }

    public CreateDishDto Build()
    {
        return new CreateDishDto
        {
            Name = _name,
            Photos = new List<string>(),
            PortionSize = _portionSize,
            Category = _category,
            Flags = _flags,
            Ingredients = _ingredients
        };
    }
}