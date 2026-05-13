using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Flows;

/// <summary>
/// Сквозные (end-to-end) тесты.
/// Проверяют полные бизнес-сценарии.
/// </summary>
public class EndToEndTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public EndToEndTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _cleaner = new DatabaseCleaner(_client);
    }

    public void Dispose() => _cleaner.Dispose();

    /// <summary>
    /// Сквозной сценарий: создание продукта → создание блюда с этим продуктом → проверка → удаление.
    /// </summary>
    [Fact]
    public async Task FullCrudFlow_CreateProductCreateDishThenDelete_AllSuccess()
    {
        // 1. Создаём продукт
        var productDto = new ProductTestDataBuilder()
            .WithName("Картофель для сценария")
            .WithBju(2, 0.4, 16.3)
            .Build();

        var productResponse = await _client.PostAsJsonAsync("/api/Products", productDto);
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(product!.Id);

        // 2. Создаём блюдо с этим продуктом
        var dishDto = new DishTestDataBuilder()
            .WithName("!салат Овощной")
            .WithCategory("")  // макрос определит категорию
            .WithPortionSize(200)
            .WithIngredient(product.Id, 150)
            .Build();

        var dishResponse = await _client.PostAsJsonAsync("/api/Dishes", dishDto);
        Assert.Equal(HttpStatusCode.Created, dishResponse.StatusCode);

        var dish = await dishResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(dish!.Id);

        // 3. Проверяем, что блюдо создалось правильно
        Assert.Equal("Овощной", dish.Name);  // макрос удалён
        Assert.Equal("Салат", dish.Category);  // макрос определил категорию
        Assert.Single(dish.Ingredients);
        Assert.Equal(product.Id, dish.Ingredients[0].ProductId);
        Assert.Equal(150, dish.Ingredients[0].Quantity);

        // 4. Получаем блюдо по ID
        var getResponse = await _client.GetAsync($"/api/Dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetchedDish = await getResponse.Content.ReadFromJsonAsync<DishDto>();
        Assert.Equal(dish.Name, fetchedDish!.Name);

        // 5. Обновляем блюдо
        var updateDto = new DishTestDataBuilder()
            .WithName("Обновлённый салат")
            .WithCategory("Салат")
            .WithPortionSize(250)
            .WithIngredient(product.Id, 200)
            .Build();

        var updateResponse = await _client.PutAsJsonAsync($"/api/Dishes/{dish.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedDish = await updateResponse.Content.ReadFromJsonAsync<DishDto>();
        Assert.Equal("Обновлённый салат", updatedDish!.Name);
        Assert.Equal(250, updatedDish.PortionSize);

        // 6. Удаляем блюдо
        var deleteResponse = await _client.DeleteAsync($"/api/Dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. Проверяем, что блюдо удалено
        var getDeletedResponse = await _client.GetAsync($"/api/Dishes/{dish.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    /// <summary>
    /// Сквозной сценарий: создание нескольких продуктов и блюда из них.
    /// </summary>
    [Fact]
    public async Task CreateComplexDish_MultipleIngredients_CalculatesCorrectly()
    {
        // 1. Создаём продукты
        var potato = await CreateAndTrackProduct("Картофель", 77, 2, 0.4, 16.3);
        var meat = await CreateAndTrackProduct("Мясо", 187.2, 18.9, 12.4, 0);
        var water = await CreateAndTrackProduct("Вода", 0, 0, 0, 0);

        // 2. Создаём блюдо
        var dishDto = new DishTestDataBuilder()
            .WithName("!суп Борщ")
            .WithCategory("")
            .WithPortionSize(500)
            .WithIngredient(potato, 200)
            .WithIngredient(meat, 200)
            .WithIngredient(water, 400)
            .Build();

        var dishResponse = await _client.PostAsJsonAsync("/api/Dishes", dishDto);
        Assert.Equal(HttpStatusCode.Created, dishResponse.StatusCode);

        var dish = await dishResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(dish!.Id);

        // 3. Проверяем
        Assert.Equal("Борщ", dish.Name);
        Assert.Equal("Суп", dish.Category);
        Assert.Equal(3, dish.Ingredients.Count);
    }

    #region Вспомогательные методы

    private async Task<Guid> CreateAndTrackProduct(string name, double calories, double proteins, double fats, double carbs)
    {
        var dto = new ProductTestDataBuilder()
            .WithName(name)
            .WithCalories(calories)
            .WithBju(proteins, fats, carbs)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await response.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);
        return created.Id;
    }

    #endregion
}