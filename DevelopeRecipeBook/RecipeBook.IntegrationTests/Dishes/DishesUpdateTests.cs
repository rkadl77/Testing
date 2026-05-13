using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Dishes;

/// <summary>
/// Тесты для удаления продукта из блюда (через PUT запрос).
/// </summary>
public class DishesRemoveIngredientTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public DishesRemoveIngredientTests(IntegrationTestFixture fixture)
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

    /// <summary>
    /// Тест: удаление продукта из блюда.
    /// Отправляем PUT запрос с новым списком ингредиентов (без удаляемого продукта).
    /// </summary>
    [Fact]
    public async Task RemoveIngredientFromDish_UpdateWithFewerIngredients_RemovesIngredient()
    {
        // создаём продукты
        var potatoId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var meatId = await CreateTestProduct("Мясо", 18.9, 12.4, 0);

        // Создаём блюдо с двумя ингредиентами
        var createDto = new DishTestDataBuilder()
            .WithName("Борщ с мясом")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(potatoId, 200)
            .WithIngredient(meatId, 150)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Dishes", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);

        Assert.Equal(2, created.Ingredients.Count);

        // обновляем блюдо, убираем мясо (оставляем только картофель)
        var updateDto = new DishTestDataBuilder()
            .WithName("Борщ без мяса")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(potatoId, 200)  // только картофель
            .Build();

        var updateResponse = await _client.PutAsJsonAsync($"/api/Dishes/{created.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<DishDto>();
        Assert.Single(updated!.Ingredients);  // должен быть только 1 ингредиент
        Assert.Equal(potatoId, updated.Ingredients[0].ProductId);
        Assert.Equal(200, updated.Ingredients[0].Quantity);
    }

    /// <summary>
    /// Тест: удаление ВСЕХ продуктов из блюда (должно вернуть ошибку, так как блюдо должно иметь минимум 1 продукт).
    /// </summary>
    [Fact]
    public async Task RemoveAllIngredientsFromDish_ReturnsBadRequest()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);

        var createDto = new DishTestDataBuilder()
            .WithName("Суп")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(productId, 200)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Dishes", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);

        // обновляем блюдо, убираем ВСЕ ингредиенты
        var updateDto = new DishTestDataBuilder()
            .WithName("Суп")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .Build();

        var updateResponse = await _client.PutAsJsonAsync($"/api/Dishes/{created.Id}", updateDto);

        // должно быть BadRequest, так как блюдо не может быть без ингредиентов
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    /// <summary>
    /// Негативный тест: удаление продукта, который используется в блюде (должно быть запрещено).
    /// </summary>
    [Fact]
    public async Task DeleteProductThatIsUsedInDish_ReturnsBadRequest()
    {
        // создаём продукт
        var productDto = new ProductTestDataBuilder()
            .WithName("Картофель для удаления")
            .WithBju(2, 0.4, 16.3)
            .Build();

        var productResponse = await _client.PostAsJsonAsync("/api/Products", productDto);
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(product!.Id);

        // Создаём блюдо с этим продуктом
        var dishDto = new DishTestDataBuilder()
            .WithName("Суп с картофелем")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(product.Id, 200)
            .Build();

        var dishResponse = await _client.PostAsJsonAsync("/api/Dishes", dishDto);
        var dish = await dishResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(dish!.Id);

        // пытаемся удалить продукт, который используется в блюде
        var deleteResponse = await _client.DeleteAsync($"/api/Products/{product.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
        var error = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("Нельзя удалить продукт", error);
        Assert.Contains(dish.Name, error);
    }
}