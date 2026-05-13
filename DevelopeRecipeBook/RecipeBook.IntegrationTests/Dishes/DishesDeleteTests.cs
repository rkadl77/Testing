using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Dishes;

/// <summary>
/// Интеграционные тесты для DELETE /api/Dishes/{id}.
/// </summary>
public class DishesDeleteTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public DishesDeleteTests(IntegrationTestFixture fixture)
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

    private async Task<Guid> CreateTestDish(string name, Guid productId, double quantity)
    {
        var dto = new DishTestDataBuilder()
            .WithName(name)
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(productId, quantity)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);
        return created.Id;
    }

    #endregion

    #region Эквивалентное разбиение - DELETE /api/Dishes/{id}

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: удаление существующего блюда.
    /// </summary>
    [Fact]
    public async Task DeleteDish_ExistingId_ReturnsNoContent()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish("Борщ", productId, 200);

        var response = await _client.DeleteAsync($"/api/Dishes/{dishId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Проверяем, что блюдо действительно удалено
        var getResponse = await _client.GetAsync($"/api/Dishes/{dishId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: удаление несуществующего блюда.
    /// </summary>
    [Fact]
    public async Task DeleteDish_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/Dishes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}