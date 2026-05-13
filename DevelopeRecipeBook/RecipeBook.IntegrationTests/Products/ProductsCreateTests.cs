using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Products;

/// <summary>
/// Интеграционные тесты для POST /api/Products.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class ProductsCreateTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public ProductsCreateTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _cleaner = new DatabaseCleaner(_client);
    }

    public void Dispose() => _cleaner.Dispose();

    #region Эквивалентное разбиение

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: корректные данные продукта.
    /// </summary>
    [Fact]
    public async Task CreateProduct_ValidData_ReturnsCreated()
    {
        var dto = new ProductTestDataBuilder()
            .WithName("Интеграционный тест")
            .WithBju(10, 5, 20)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(dto.Name, created.Name);
        Assert.Equal(dto.Calories, created.Calories);
        Assert.Equal(dto.Proteins, created.Proteins);
        Assert.Equal(dto.Fats, created.Fats);
        Assert.Equal(dto.Carbs, created.Carbs);

        _cleaner.TrackProduct(created.Id);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: сумма БЖУ превышает 100 (физически невозможно).
    /// </summary>
    [Fact]
    public async Task CreateProduct_BjuSumExceeds100_ReturnsBadRequest()
    {
        // сумма = 60 + 50 + 50 = 160 > 100
        var dto = new ProductTestDataBuilder()
            .WithName("Невозможный продукт")
            .WithBju(60, 50, 50)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Сумма БЖУ на 100г не может превышать 100", error);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: пустое название (нарушение обязательного поля).
    /// </summary>
    [Fact]
    public async Task CreateProduct_EmptyName_ReturnsBadRequest()
    {
        var dto = new ProductTestDataBuilder()
            .WithName("")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: название короче 2 символов.
    /// </summary>
    [Fact]
    public async Task CreateProduct_NameTooShort_ReturnsBadRequest()
    {
        var dto = new ProductTestDataBuilder()
            .WithName("A")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: отрицательные значения КБЖУ.
    /// </summary>
    [Fact]
    public async Task CreateProduct_NegativeValues_ReturnsBadRequest()
    {
        var dto = new ProductTestDataBuilder()
            .WithName("Отрицательные значения")
            .WithBju(-10, -5, -20)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Анализ граничных значений

    /// <summary>
    /// Граничные значения: сумма БЖУ = 100 (максимально допустимая).
    /// </summary>
    [Fact]
    public async Task CreateProduct_BjuSumEquals100_ReturnsCreated()
    {
        // сумма = 50 + 30 + 20 = 100
        var dto = new ProductTestDataBuilder()
            .WithName("Максимальный БЖУ продукт")
            .WithBju(50, 30, 20)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _cleaner.TrackProduct(created!.Id);
    }

    /// <summary>
    /// Граничные значения: сумма БЖУ = 101 (чуть выше лимита).
    /// </summary>
    [Fact]
    public async Task CreateProduct_BjuSumEquals101_ReturnsBadRequest()
    {
        // сумма = 51 + 30 + 20 = 101 > 100
        var dto = new ProductTestDataBuilder()
            .WithName("Превышающий продукт")
            .WithBju(51, 30, 20)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Граничные значения: название из 2 символов (минимальная длина).
    /// </summary>
    [Fact]
    public async Task CreateProduct_NameWithMinLength_ReturnsCreated()
    {
        var dto = new ProductTestDataBuilder()
            .WithName("АБ")
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _cleaner.TrackProduct(created!.Id);
    }

    #endregion
}