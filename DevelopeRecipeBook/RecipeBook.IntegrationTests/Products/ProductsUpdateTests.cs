using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Products;

/// <summary>
/// Интеграционные тесты для PUT /api/Products/{id}.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class ProductsUpdateTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public ProductsUpdateTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _cleaner = new DatabaseCleaner(_client);
    }

    public void Dispose() => _cleaner.Dispose();

    #region Эквивалентное разбиение

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: корректные данные для обновления.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_ValidData_ReturnsOk()
    {
        // создаём продукт
        var createDto = new ProductTestDataBuilder()
            .WithName($"UpdateTest_{Guid.NewGuid()}")
            .WithBju(10, 5, 20)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        // обновляем
        var updateDto = new ProductTestDataBuilder()
            .WithName("Обновлённое название")
            .WithBju(20, 10, 30)
            .WithCalories(250)
            .Build();

        var response = await _client.PutAsJsonAsync($"/api/Products/{created.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Обновлённое название", updated!.Name);
        Assert.Equal(250, updated.Calories);
        Assert.Equal(20, updated.Proteins);
        Assert.Equal(10, updated.Fats);
        Assert.Equal(30, updated.Carbs);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: обновление с суммой БЖУ > 100.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_BjuSumExceeds100_ReturnsBadRequest()
    {
        var createDto = new ProductTestDataBuilder()
            .WithName($"UpdateInvalid_{Guid.NewGuid()}")
            .WithBju(10, 5, 10)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        // сумма = 60 + 50 + 50 = 160 > 100
        var updateDto = new ProductTestDataBuilder()
            .WithName("Невозможное обновление")
            .WithBju(60, 50, 50)
            .Build();

        var response = await _client.PutAsJsonAsync($"/api/Products/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Сумма БЖУ на 100г не может превышать 100", error);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: обновление несуществующего продукта.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_NonExistingId_ReturnsNotFound()
    {
        var updateDto = new ProductTestDataBuilder()
            .WithName("Несуществующий продукт")
            .Build();

        var response = await _client.PutAsJsonAsync($"/api/Products/{Guid.NewGuid()}", updateDto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Анализ граничных значений

    /// <summary>
    /// Граничные значения: обновление с суммой БЖУ = 100.
    /// </summary>
    [Fact]
    public async Task UpdateProduct_BjuSumEquals100_ReturnsOk()
    {
        var createDto = new ProductTestDataBuilder()
            .WithName($"UpdateBoundary_{Guid.NewGuid()}")
            .WithBju(10, 5, 10)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        // сумма = 50 + 30 + 20 = 100
        var updateDto = new ProductTestDataBuilder()
            .WithName("Граничный продукт")
            .WithBju(50, 30, 20)
            .Build();

        var response = await _client.PutAsJsonAsync($"/api/Products/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(50, updated!.Proteins);
        Assert.Equal(30, updated.Fats);
        Assert.Equal(20, updated.Carbs);
    }

    /// <summary>
    /// Граничные значения: обновление с названием из 2 символов (мин. длина).
    /// </summary>
    [Fact]
    public async Task UpdateProduct_NameWithMinLength_ReturnsOk()
    {
        var createDto = new ProductTestDataBuilder()
            .WithName($"UpdateMinLength_{Guid.NewGuid()}")
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        var updateDto = new ProductTestDataBuilder()
            .WithName("АБ")
            .Build();

        var response = await _client.PutAsJsonAsync($"/api/Products/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}