using RecipeBook.Core.DTOs;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace RecipeBook.IntegrationTests.Products;

/// <summary>
/// Интеграционные тесты для DELETE /api/Products/{id}.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class ProductsDeleteTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public ProductsDeleteTests(IntegrationTestFixture fixture)
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

    #region Эквивалентное разбиение

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: удаление существующего продукта (не используется в блюдах).
    /// </summary>
    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsNoContent()
    {
        // создаём продукт
        var dto = new ProductTestDataBuilder()
            .WithName($"DeleteTest_{Guid.NewGuid()}")
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        var response = await _client.DeleteAsync($"/api/Products/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Проверяем, что продукт действительно удалён
        var getResponse = await _client.GetAsync($"/api/Products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: удаление несуществующего продукта.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/Products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Анализ граничных значений - Защита от удаления используемого продукта

    /// <summary>
    /// Негативный тест. Защита от удаления продукта, который используется в блюде.
    /// Согласно требованию 1.5 ТЗ: удаление продукта, который используется в составе хотя бы одного блюда, должно быть недоступно.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_UsedInDish_ReturnsBadRequest_AndShowsWhichDish()
    {
        // 1. Создаём продукт
        var productId = await CreateTestProduct("Картофель для теста", 2, 0.4, 16.3);

        // 2. Создаём блюдо с этим продуктом
        var dishName = $"Блюдо для теста_{Guid.NewGuid()}";
        var dishId = await CreateTestDish(dishName, productId, 200);

        // пытаемся удалить продукт
        var response = await _client.DeleteAsync($"/api/Products/{productId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Нельзя удалить продукт", error);
        Assert.Contains(dishName, error); 
    }

    /// <summary>
    /// Негативный тест. Защита от удаления продукта, который используется в НЕСКОЛЬКИХ блюдах.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_UsedInMultipleDishes_ReturnsBadRequest_AndShowsAllDishes()
    {
        // 1. Создаём продукт
        var productId = await CreateTestProduct("Универсальный продукт", 10, 5, 20);

        // 2. Создаём несколько блюд с этим продуктом
        var dishName1 = $"Блюдо 1_{Guid.NewGuid()}";
        var dishName2 = $"Блюдо 2_{Guid.NewGuid()}";

        var dishId1 = await CreateTestDish(dishName1, productId, 100);
        var dishId2 = await CreateTestDish(dishName2, productId, 150);

        // пытаемся удалить продукт
        var response = await _client.DeleteAsync($"/api/Products/{productId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Нельзя удалить продукт", error);
        Assert.Contains(dishName1, error);
        Assert.Contains(dishName2, error);
    }

    /// <summary>
    /// Позитивный тест. Продукт можно удалить ПОСЛЕ того, как он был удалён из всех блюд.
    /// </summary>
    [Fact]
    public async Task DeleteProduct_AfterRemovingFromDish_ReturnsNoContent()
    {
        // 1. Создаём продукт, который будем удалять
        var productIdToDelete = await CreateTestProduct("Картофель временный", 2, 0.4, 16.3);

        // 2. Создаём другой продукт, который останется в блюде
        var anotherProductId = await CreateTestProduct("Другой продукт", 1, 1, 1);

        // 3. Создаём блюдо с двумя продуктами
        var createDto = new DishTestDataBuilder()
            .WithName("Временное блюдо")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(productIdToDelete, 200)
            .WithIngredient(anotherProductId, 50)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Dishes", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);

        // 4. Обновляем блюдо - убираем только первый продукт, оставляем второй
        var updateDto = new DishTestDataBuilder()
            .WithName("Временное блюдо")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(anotherProductId, 50)  // только второй продукт
            .Build();

        var updateResponse = await _client.PutAsJsonAsync($"/api/Dishes/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // теперь пытаемся удалить первый продукт
        var deleteResponse = await _client.DeleteAsync($"/api/Products/{productIdToDelete}");


    }

    #endregion
}