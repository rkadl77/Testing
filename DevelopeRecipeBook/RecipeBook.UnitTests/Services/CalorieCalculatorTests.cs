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

    /// <summary>
    /// негативный тест
    /// Эквивалентное разбиение. Класс: нет продуктов (пустой состав).
    /// </summary>
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

    /// <summary>
    /// Негативный тест.
    /// Проверяет, что при передаче null в DishProducts (вместо списка продуктов)
    /// калькулятор не выбрасывает исключение, а возвращает нулевое КБЖУ (0,0,0,0).
    /// </summary>
    /// 
    [Fact]
    public void Calculate_NullIngredients_ReturnsZero()
    {
        var dish = new Dish { DishProducts = null! };
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(0, c);
        Assert.Equal(0, p);
        Assert.Equal(0, f);
        Assert.Equal(0, cb);
    }

    /// <summary>
    /// Эквивалентное разбиение. Класс: один продукт (50г, 100г, 200г, 75г).
    /// </summary>
    [Theory]
    [InlineData(50, 200, 20, 10, 15, 100, 10, 5, 7.5)]      // 50г
    [InlineData(100, 77, 2, 0.4, 16.3, 77, 2, 0.4, 16.3)]  // 100г
    [InlineData(200, 50, 5, 2.5, 10, 100, 10, 5, 20)]      // 200г
    [InlineData(75, 100, 10, 5, 20, 75, 7.5, 3.75, 15)]    // 75г
    public void Calculate_SingleProduct_ReturnsCorrectValues(
        double q, double pCal, double pProt, double pFat, double pCarbs,
        double eCal, double eProt, double eFat, double eCarbs)
    {
        var product = CreateProduct(pCal, pProt, pFat, pCarbs);
        var dish = CreateDish((product, q));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(eCal, c);
        Assert.Equal(eProt, p);
        Assert.Equal(eFat, f);
        Assert.Equal(eCarbs, cb);
    }

    /// <summary>
    /// негативный тест
    /// Эквивалентное разбиение. Класс: нулевой продукт (вода).
    /// </summary>
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

    /// <summary>
    /// Эквивалентное разбиение. Класс: несколько продуктов (борщ из ТЗ).
    /// </summary>
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

    /// <summary>
    /// Эквивалентное разбиение. Класс: только один макронутриент (калории, белки, жиры, углеводы).
    /// </summary>
    [Theory]
    [InlineData(50, 0, 0, 0, 200, 100, 0, 0, 0)]   // только калории
    [InlineData(0, 30, 0, 0, 150, 0, 45, 0, 0)]   // только белки
    [InlineData(0, 0, 25, 0, 80, 0, 0, 20, 0)]    // только жиры
    [InlineData(0, 0, 0, 40, 250, 0, 0, 0, 100)]  // только углеводы
    public void Calculate_SingleMacronutrient_ReturnsCorrectValues(
        double cal, double prot, double fat, double carb, double q,
        double eCal, double eProt, double eFat, double eCarbs)
    {
        var product = CreateProduct(cal, prot, fat, carb);
        var dish = CreateDish((product, q));
        var (c, p, f, cb) = _calculator.Calculate(dish);
        Assert.Equal(eCal, c);
        Assert.Equal(eProt, p);
        Assert.Equal(eFat, f);
        Assert.Equal(eCarbs, cb);
    }

    #endregion

    #region Анализ граничных значений

    /// <summary>
    /// Граничные значения: 0г (нижняя граница), 0.1г (чуть выше), 100г (ровно 100), 10000г (очень много).
    /// </summary>
    [Theory]
    [InlineData(0, 100, 10, 5, 20, 0, 0, 0, 0)]           // 0г → 0
    [InlineData(0.1, 200, 20, 10, 15, 0.2, 0.02, 0.01, 0.02)]  // 0.1г → очень мало
    [InlineData(100, 77, 2, 0.4, 16.3, 77, 2, 0.4, 16.3)] // 100г → ровно
    [InlineData(10000, 50, 5, 2.5, 10, 5000, 500, 250, 1000)]   // 10000г → очень много
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

    /// <summary>
    /// Граничные значения: максимально допустимая сумма БЖУ = 100.
    /// </summary>
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

    /// <summary>
    /// Граничные значения: минимально допустимая сумма БЖУ = 0.
    /// </summary>
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

    /// <summary>
    /// негативный тест
    /// Граничные значения: отрицательное количество продукта.
    /// </summary>
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

    /// <summary>
    /// Округление: длинные десятичные дроби → округление до 2 знаков.
    /// </summary>
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

    /// <summary>
    /// Округление: ровно 0.5 → корректное округление.
    /// </summary>
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

    /// <summary>
    /// Округление: очень маленькое значение (1г продукта с 1 единицей КБЖУ) → 0.01.
    /// </summary>
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

    /// <summary>
    /// Множество продуктов: 5 разных продуктов → проверка суммирования.
    /// </summary>
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

    /// <summary>
    /// Множество продуктов: один и тот же продукт дважды → суммирование отдельных позиций.
    /// </summary>
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