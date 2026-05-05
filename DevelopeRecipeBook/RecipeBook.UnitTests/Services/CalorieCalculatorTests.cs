using RecipeBook.Core.Models;
using RecipeBook.Core.Services;

namespace RecipeBook.UnitTests.Services;

/// <summary>
/// Unit-тесты для сервиса автоматического расчёта КБЖУ блюда (CalorieCalculator).
/// Используются техники тест-дизайна:
///   - Эквивалентное разбиение (пустой состав, один продукт, несколько продуктов)
///   - Анализ граничных значений (количество = 0, 100, 0.1, 10000)
/// 
/// Дополнительно:
///   - Параметризованные тесты (Theory + InlineData) для граничных значений
///   - Setup через конструктор (создание экземпляра CalorieCalculator)
///   - Вспомогательный фабричный метод CreateProduct для чистоты Arrange
/// </summary>
public class CalorieCalculatorTests
{
    private readonly CalorieCalculator _calculator;

    public CalorieCalculatorTests()
    {
        // Setup: выполняется перед каждым тестом
        _calculator = new CalorieCalculator();
    }

    #region Вспомогательные методы (фабрики)

    /// <summary>
    /// Создаёт продукт с указанными КБЖУ. Остальные поля — значения по умолчанию.
    /// </summary>
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

    /// <summary>
    /// Создаёт блюдо из списка кортежей (продукт, количество в граммах).
    /// </summary>
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
    /// Класс эквивалентности: пустой состав блюда.
    /// Ожидаемый результат: КБЖУ = (0, 0, 0, 0).
    /// </summary>
    [Fact]
    public void Calculate_EmptyIngredients_ReturnsZero()
    {
        // Arrange
        var dish = new Dish { DishProducts = new List<DishProduct>() };

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert
        Assert.Equal(0, calories);
        Assert.Equal(0, proteins);
        Assert.Equal(0, fats);
        Assert.Equal(0, carbs);
    }

    /// <summary>
    /// Класс эквивалентности: null-состав блюда (защита от NullReferenceException).
    /// Ожидаемый результат: КБЖУ = (0, 0, 0, 0).
    /// </summary>
    [Fact]
    public void Calculate_NullIngredients_ReturnsZero()
    {
        // Arrange
        var dish = new Dish { DishProducts = null! };

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert
        Assert.Equal(0, calories);
        Assert.Equal(0, proteins);
        Assert.Equal(0, fats);
        Assert.Equal(0, carbs);
    }

    /// <summary>
    /// Класс эквивалентности: один продукт с ненулевыми КБЖУ.
    /// Ожидаемый результат: КБЖУ пропорциональны количеству (quantity / 100).
    /// </summary>
    [Fact]
    public void Calculate_SingleProduct_ReturnsCorrectValues()
    {
        // Arrange
        var product = CreateProduct(calories: 200, proteins: 20, fats: 10, carbs: 15);
        var dish = CreateDish((product, 50));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert: коэффициент 50/100 = 0.5
        Assert.Equal(100, calories);
        Assert.Equal(10, proteins);
        Assert.Equal(5, fats);
        Assert.Equal(7.5, carbs);
    }

    /// <summary>
    /// Класс эквивалентности: продукт с нулевыми КБЖУ (например, вода) в составе с обычным продуктом.
    /// Ожидаемый результат: нулевые КБЖУ от воды, обычные — от второго продукта.
    /// </summary>
    [Fact]
    public void Calculate_ProductWithZeroNutrition_ReturnsZeroForThatProduct()
    {
        // Arrange
        var water = CreateProduct(calories: 0, proteins: 0, fats: 0, carbs: 0);
        var potato = CreateProduct(calories: 77, proteins: 2, fats: 0.4, carbs: 16.3);
        var dish = CreateDish((water, 300), (potato, 200));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert: вода не даёт вклада, картофель: 77*2, 2*2, 0.4*2, 16.3*2
        Assert.Equal(154, calories);
        Assert.Equal(4, proteins);
        Assert.Equal(0.8, fats);
        Assert.Equal(32.6, carbs);
    }

    /// <summary>
    /// Класс эквивалентности: несколько продуктов (реальный пример — борщ из ТЗ).
    /// Проверка на соответствие эталонным значениям КБЖУ.
    /// </summary>
    [Fact]
    public void Calculate_MultipleProducts_ReturnsCorrectSum()
    {
        // Arrange
        var potato = CreateProduct(calories: 77, proteins: 2, fats: 0.4, carbs: 16.3);
        var water = CreateProduct(calories: 0, proteins: 0, fats: 0, carbs: 0);
        var meat = CreateProduct(calories: 187.2, proteins: 18.9, fats: 12.4, carbs: 0);
        var dish = CreateDish((potato, 150), (water, 250), (meat, 100));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert: как в ТЗ для борща
        Assert.Equal(302.7, calories);
        Assert.Equal(21.9, proteins);
        Assert.Equal(13, fats);
        Assert.Equal(24.45, carbs);
    }

    #endregion

    #region Анализ граничных значений (параметризованный тест)

    /// <summary>
    /// Параметризованный тест граничных значений количества продукта.
    /// Проверяет, что КБЖУ корректно пересчитываются при:
    ///   — quantity = 0 (нижняя граница, нулевой вклад)
    ///   — quantity = 0.1 (очень маленькое значение)
    ///   — quantity = 100 (ровно 100 г — коэффициент 1.0)
    ///   — quantity = 10000 (очень большое значение)
    /// </summary>
    /// <param name="quantity">Количество продукта в граммах.</param>
    /// <param name="productCal">Калорийность продукта на 100 г.</param>
    /// <param name="productProt">Белки продукта на 100 г.</param>
    /// <param name="productFat">Жиры продукта на 100 г.</param>
    /// <param name="productCarbs">Углеводы продукта на 100 г.</param>
    /// <param name="expectedCal">Ожидаемая калорийность порции.</param>
    /// <param name="expectedProt">Ожидаемые белки порции.</param>
    /// <param name="expectedFat">Ожидаемые жиры порции.</param>
    /// <param name="expectedCarbs">Ожидаемые углеводы порции.</param>
    [Theory]
    [InlineData(0, 100, 10, 5, 20, 0, 0, 0, 0)]        // нулевая quantity
    [InlineData(0.1, 200, 20, 10, 15, 0.2, 0.02, 0.01, 0.02)]     // очень мало
    [InlineData(100, 77, 2, 0.4, 16.3, 77, 2, 0.4, 16.3)]     // ровно 100
    [InlineData(10000, 50, 5, 2.5, 10, 5000, 500, 250, 1000)]     // очень много
    public void Calculate_QuantityBoundaries_ReturnsCorrectValues(
        double quantity,
        double productCal, double productProt, double productFat, double productCarbs,
        double expectedCal, double expectedProt, double expectedFat, double expectedCarbs)
    {
        // Arrange
        var product = CreateProduct(productCal, productProt, productFat, productCarbs);
        var dish = CreateDish((product, quantity));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert
        Assert.Equal(expectedCal, calories);
        Assert.Equal(expectedProt, proteins);
        Assert.Equal(expectedFat, fats);
        Assert.Equal(expectedCarbs, carbs);
    }

    /// <summary>
    /// Граничное значение: продукт с БЖУ = 100 (максимально допустимая сумма).
    /// Проверка, что граница корректно пересчитывается без ошибок округления.
    /// </summary>
    [Fact]
    public void Calculate_MaxBjuProduct_ReturnsCorrectValues()
    {
        // Arrange
        var product = CreateProduct(calories: 400, proteins: 50, fats: 30, carbs: 20);
        var dish = CreateDish((product, 200));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert: коэффициент 200/100 = 2
        Assert.Equal(800, calories);
        Assert.Equal(100, proteins);
        Assert.Equal(60, fats);
        Assert.Equal(40, carbs);
    }

    #endregion

    #region Округление

    /// <summary>
    /// Проверка округления КБЖУ до двух знаков после запятой.
    /// Используются значения, дающие длинные десятичные дроби при умножении.
    /// </summary>
    [Fact]
    public void Calculate_RoundingToTwoDecimalPlaces()
    {
        // Arrange
        var product = CreateProduct(calories: 100, proteins: 33.3333, fats: 33.3333, carbs: 33.3333);
        var dish = CreateDish((product, 33.33));

        // Act
        var (calories, proteins, fats, carbs) = _calculator.Calculate(dish);

        // Assert: округление до 2 знаков
        Assert.Equal(33.33, calories);
        Assert.Equal(11.11, proteins);
        Assert.Equal(11.11, fats);
        Assert.Equal(11.11, carbs);
    }

    #endregion
}