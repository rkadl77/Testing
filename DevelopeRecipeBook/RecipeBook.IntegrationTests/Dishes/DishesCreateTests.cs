using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Dishes;

/// <summary>
/// Интеграционные тесты для POST /api/Dishes.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class DishesCreateTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public DishesCreateTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _cleaner = new DatabaseCleaner(_client);
    }

    public void Dispose() => _cleaner.Dispose();

    #region Вспомогательные методы

    private async Task<Guid> CreateTestProduct(string name, double proteins, double fats, double carbs)
    {
        var dto = new ProductTestDataBuilder()
            .WithName(name)
            .WithBju(proteins, fats, carbs)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);
        return created.Id;
    }

    #endregion

    #region Эквивалентное разбиение

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: корректные данные блюда.
    /// </summary>
    [Fact]
    public async Task CreateDish_ValidData_ReturnsCreated()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var dto = new DishTestDataBuilder()
            .WithName("Борщ")
            .WithPortionSize(500)
            .WithIngredient(productId, 200)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Борщ", created.Name);
        Assert.Single(created.Ingredients);
        _cleaner.TrackDish(created.Id);
    }

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: макрос в названии определяет категорию.
    /// </summary>
    [Theory]
    [InlineData("!суп Борщ", "Суп")]
    [InlineData("!салат Цезарь", "Салат")]
    [InlineData("!десерт Торт", "Десерт")]
    public async Task CreateDish_WithMacro_SetsCategory(string name, string expectedCategory)
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var dto = new DishTestDataBuilder()
            .WithName(name)
            .WithCategory("")
            .WithIngredient(productId, 200)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(expectedCategory, created!.Category);
        Assert.DoesNotContain("!", created.Name);
        _cleaner.TrackDish(created.Id);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: пустое название блюда.
    /// </summary>
    [Fact]
    public async Task CreateDish_EmptyName_ReturnsBadRequest()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var dto = new DishTestDataBuilder()
            .WithName("")
            .WithIngredient(productId, 200)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: нет ингредиентов.
    /// </summary>
    [Fact]
    public async Task CreateDish_NoIngredients_ReturnsBadRequest()
    {
        var dto = new DishTestDataBuilder()
            .WithName("Пустое блюдо")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Анализ граничных значений

    /// <summary>
    /// Граничные значения: название из 2 символов (мин. длина).
    /// </summary>
    [Fact]
    public async Task CreateDish_NameWithMinLength_ReturnsCreated()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var dto = new DishTestDataBuilder()
            .WithName("Бя")
            .WithIngredient(productId, 200)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _cleaner.TrackDish(created!.Id);
    }

    /// <summary>
    /// Граничные значения: порция = 0.1г (минимальное значение).
    /// </summary>
    [Fact]
    public async Task CreateDish_PortionSizeMinValue_ReturnsCreated()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var dto = new DishTestDataBuilder()
            .WithName("Микро блюдо")
            .WithPortionSize(0.1)
            .WithIngredient(productId, 0.1)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _cleaner.TrackDish(created!.Id);
    }

    #endregion
}