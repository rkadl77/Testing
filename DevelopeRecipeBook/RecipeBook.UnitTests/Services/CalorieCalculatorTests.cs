using RecipeBook.Core.Models;
using RecipeBook.Core.Services;

namespace RecipeBook.UnitTests.Services;

public class CalorieCalculatorTests
{
    private readonly CalorieCalculator _calculator;

    public CalorieCalculatorTests()
    {
        _calculator = new CalorieCalculator();
    }

    #region Фабрики

    private static Product CreateProduct(double calories, double proteins, double fats, double carbs)
    {
        return new Product
        {
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbs = carbs
        };
    }

    private static Dish CreateDish(params (Product product, double quantity)[] ingredients)
    {
        return new Dish
        {
            DishProducts = ingredients
                .Select(i => new DishProduct { Product = i.product, Quantity = i.quantity })
                .ToList()
        };
    }

    #endregion

    #region Эквивалентное разбиение

    /// <summary>Пустой состав → КБЖУ = (0, 0, 0, 0). Класс: нет продуктов.</summary>
    [Fact]
    public void Calculate_EmptyIngredients_ReturnsZero()
    {
        var dish = new Dish { DishProducts = new List<DishProduct>() };
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(0, cb);
    }

    /// <summary>Null-состав → нет исключения, КБЖУ = (0, 0, 0, 0). Класс: null.</summary>
    [Fact]
    public void Calculate_NullIngredients_ReturnsZero()
    {
        var dish = new Dish { DishProducts = null! };
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(0, c);
        Assert.Equal(0, cb);
    }

    /// <summary>Один продукт 50g → КБЖУ = значения × 0.5. Класс: один продукт.</summary>
    [Fact]
    public void Calculate_SingleProduct_ReturnsCorrectValues()
    {
        var product = CreateProduct(200, 20, 10, 15);
        var dish = CreateDish((product, 50));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(100, c);
        Assert.Equal(10, p);
        Assert.Equal(5, f);
        Assert.Equal(7.5, cb);
    }

    /// <summary>Вода (КБЖУ=0) + картофель → вода не даёт вклада. Класс: нулевой продукт.</summary>
    [Fact]
    public void Calculate_ProductWithZeroNutrition_ReturnsZeroForThatProduct()
    {
        var water = CreateProduct(0, 0, 0, 0);
        var potato = CreateProduct(77, 2, 0.4, 16.3);
        var dish = CreateDish((water, 300), (potato, 200));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(154, c);
        Assert.Equal(4, p);
        Assert.Equal(0.8, f);
        Assert.Equal(32.6, cb);
    }

    /// <summary>Борщ из ТЗ (3 продукта) → проверка эталонных КБЖУ. Класс: несколько продуктов.</summary>
    [Fact]
    public void Calculate_MultipleProducts_ReturnsCorrectSum()
    {
        var potato = CreateProduct(77, 2, 0.4, 16.3);
        var water = CreateProduct(0, 0, 0, 0);
        var meat = CreateProduct(187.2, 18.9, 12.4, 0);
        var dish = CreateDish((potato, 150), (water, 250), (meat, 100));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(302.7, c);
        Assert.Equal(21.9, p);
        Assert.Equal(13, f);
        Assert.Equal(24.45, cb);
    }

    /// <summary>Только калории — белки, жиры, углеводы = 0. Класс: один макронутриент.</summary>
    [Fact]
    public void Calculate_ProductWithOnlyCalories_ReturnsOnlyCalories()
    {
        var product = CreateProduct(50, 0, 0, 0);
        var dish = CreateDish((product, 200));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(100, c);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(0, cb);
    }

    /// <summary>Только белки — остальные макронутриенты = 0.</summary>
    [Fact]
    public void Calculate_ProductWithOnlyProteins_ReturnsOnlyProteins()
    {
        var product = CreateProduct(0, 30, 0, 0);
        var dish = CreateDish((product, 150));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(45, p);
        Assert.Equal(0, f);
        Assert.Equal(0, cb);
    }

    /// <summary>Только жиры — остальные макронутриенты = 0.</summary>
    [Fact]
    public void Calculate_ProductWithOnlyFats_ReturnsOnlyFats()
    {
        var product = CreateProduct(0, 0, 25, 0);
        var dish = CreateDish((product, 80));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(0, p);
        Assert.Equal(20, f);
        Assert.Equal(0, cb);
    }

    /// <summary>Только углеводы — остальные макронутриенты = 0.</summary>
    [Fact]
    public void Calculate_ProductWithOnlyCarbs_ReturnsOnlyCarbs()
    {
        var product = CreateProduct(0, 0, 0, 40);
        var dish = CreateDish((product, 250));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(100, cb);
    }

    #endregion

    #region Анализ граничных значений

    /// <summary>Граничные количества: 0g (низ), 0.1g (очень мало), 100g (ровно 100), 10000g (очень много).</summary>
    // параметризованный тест 
    [Theory]
    [InlineData(0, 100, 10, 5, 20, 0, 0, 0, 0)]
    [InlineData(0.1, 200, 20, 10, 15, 0.2, 0.02, 0.01, 0.02)]
    [InlineData(100, 77, 2, 0.4, 16.3, 77, 2, 0.4, 16.3)]
    [InlineData(10000, 50, 5, 2.5, 10, 5000, 500, 250, 1000)]
    public void Calculate_QuantityBoundaries_ReturnsCorrectValues(
        double q, double pCal, double pProt, double pFat, double pCarbs,
        double eCal, double eProt, double eFat, double eCarbs)
    {
        var dish = CreateDish((CreateProduct(pCal, pProt, pFat, pCarbs), q));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(eCal, c);
        Assert.Equal(eProt, p);
        Assert.Equal(eFat, f);
        Assert.Equal(eCarbs, cb);
    }

    /// <summary>Разные количества продукта → проверка пропорциональности расчёта.</summary>
    // параметризованный тест 
    [Theory]
    [InlineData(50, 200, 20, 10, 15, 100, 10, 5, 7.5)]
    [InlineData(100, 77, 2, 0.4, 16.3, 77, 2, 0.4, 16.3)]
    [InlineData(200, 50, 5, 2.5, 10, 100, 10, 5, 20)]
    [InlineData(75, 100, 10, 5, 20, 75, 7.5, 3.75, 15)]
    [InlineData(333.33, 30, 3, 3, 3, 100, 10, 10, 10)]
    public void Calculate_DifferentQuantities_ReturnsProportionalValues(
        double q, double pCal, double pProt, double pFat, double pCarbs,
        double eCal, double eProt, double eFat, double eCarbs)
    {
        var dish = CreateDish((CreateProduct(pCal, pProt, pFat, pCarbs), q));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(eCal, c);
        Assert.Equal(eProt, p);
        Assert.Equal(eFat, f);
        Assert.Equal(eCarbs, cb);
    }

    /// <summary>Продукт с БЖУ = 100 (максимально допустимая сумма) → корректный пересчёт.</summary>
    [Fact]
    public void Calculate_MaxBjuProduct_ReturnsCorrectValues()
    {
        var product = CreateProduct(400, 50, 30, 20);
        var dish = CreateDish((product, 200));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(800, c);
        Assert.Equal(100, p);
        Assert.Equal(60, f);
        Assert.Equal(40, cb);
    }

    /// <summary>Продукт с БЖУ = 0 (нижняя граница) → все значения 0.</summary>
    [Fact]
    public void Calculate_MinBjuProduct_ReturnsZero()
    {
        var product = CreateProduct(0, 0, 0, 0);
        var dish = CreateDish((product, 500));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(0, cb);
    }

    /// <summary>Отрицательное количество продукта → система не падает, результат ≤ 0.</summary>
    [Fact]
    public void Calculate_QuantityBelowZero_HandlesCorrectly()
    {
        var product = CreateProduct(100, 10, 5, 20);
        var dish = CreateDish((product, -10));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.True(c <= 0);
    }

    #endregion

    #region Округление

    /// <summary>Длинные десятичные дроби → округление до 2 знаков.</summary>
    [Fact]
    public void Calculate_RoundingToTwoDecimalPlaces()
    {
        var product = CreateProduct(100, 33.3333, 33.3333, 33.3333);
        var dish = CreateDish((product, 33.33));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(33.33, c);
        Assert.Equal(11.11, p);
        Assert.Equal(11.11, f);
        Assert.Equal(11.11, cb);
    }

    /// <summary>Ровно 0.5 → корректное округление без потери точности.</summary>
    [Fact]
    public void Calculate_RoundingAtHalf_HandlesCorrectly()
    {
        var product = CreateProduct(100, 10, 10, 10);
        var dish = CreateDish((product, 15));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(15, c);
        Assert.Equal(1.5, p);
        Assert.Equal(1.5, f);
        Assert.Equal(1.5, cb);
    }

    /// <summary>Очень маленькое значение (1g продукта с 1 единицей КБЖУ) → 0.01.</summary>
    [Fact]
    public void Calculate_ManyDecimalPlaces_TruncatesToTwo()
    {
        var product = CreateProduct(1, 1, 1, 1);
        var dish = CreateDish((product, 1));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0.01, c);
        Assert.Equal(0.01, p);
        Assert.Equal(0.01, f);
        Assert.Equal(0.01, cb);
    }

    #endregion

    #region Множество продуктов

    /// <summary>5 разных продуктов → проверка суммирования КБЖУ.</summary>
    [Fact]
    public void Calculate_FiveDifferentProducts_SumCorrectly()
    {
        var p1 = CreateProduct(100, 10, 5, 20);
        var p2 = CreateProduct(50, 5, 2, 10);
        var p3 = CreateProduct(200, 20, 10, 30);
        var p4 = CreateProduct(0, 0, 0, 0);
        var p5 = CreateProduct(80, 8, 4, 15);
        var dish = CreateDish((p1, 100), (p2, 50), (p3, 200), (p4, 100), (p5, 75));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(585, c);
        Assert.Equal(58.5, p);
        Assert.Equal(29, f);
        Assert.Equal(96.25, cb);
    }

    /// <summary>Один и тот же продукт дважды → суммируется как отдельные позиции.</summary>
    [Fact]
    public void Calculate_DuplicateProduct_CountsEachSeparately()
    {
        var product = CreateProduct(100, 10, 5, 20);
        var dish = CreateDish((product, 50), (product, 30));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(80, c);
        Assert.Equal(8, p);
        Assert.Equal(4, f);
        Assert.Equal(16, cb);
    }

    #endregion
}