using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Dishes;

/// <summary>
/// Интеграционные тесты для GET /api/Dishes.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class DishesReadTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public DishesReadTests(IntegrationTestFixture fixture)
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

    private async Task<Guid> CreateTestDish(string name, string category, Guid productId, double quantity)
    {
        var dto = new DishTestDataBuilder()
            .WithName(name)
            .WithCategory(category)
            .WithPortionSize(500)
            .WithIngredient(productId, quantity)
            .Build();

        var response = await _client.PostAsJsonAsync("/api/Dishes", dto);
        var created = await response.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);
        return created.Id;
    }

    #endregion

    #region Эквивалентное разбиение - GET /api/Dishes

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: получение списка всех блюд.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/Dishes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.NotNull(dishes);
    }

    /// <summary>
    /// Эквивалентное разбиение. Фильтрация по существующей категории.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_FilterByExistingCategory_ReturnsFiltered()
    {
        // Arrange - создаём блюдо с категорией "Суп"
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish("Суп куриный", "Суп", productId, 200);

        var response = await _client.GetAsync("/api/Dishes?category=Суп");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.NotNull(dishes);
        Assert.All(dishes, d => Assert.Equal("Суп", d.Category));
    }

    /// <summary>
    /// Эквивалентное разбиение. Фильтрация по несуществующей категории.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_FilterByNonExistingCategory_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/Dishes?category=НесуществующаяКатегория");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Эквивалетное разбиение. Фильтрация по флагу.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_FilterByFlag_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/Dishes?flags=Веган");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.NotNull(dishes);
        // Проверяем, что все блюда имеют флаг "Веган" (если такие есть)
        foreach (var dish in dishes)
        {
            Assert.Contains("Веган", dish.Flags);
        }
    }

    /// <summary>
    /// Эквивалентное разбиение. Поиск по существующей подстроке.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_SearchByExistingSubstring_ReturnsFiltered()
    {
        // создаём блюдо с уникальным именем
        var uniqueName = $"ПоискБлюда_{Guid.NewGuid()}";
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish(uniqueName, "Суп", productId, 200);

        var response = await _client.GetAsync($"/api/Dishes?search=ПоискБлюда");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.NotNull(dishes);
        Assert.Contains(dishes, d => d.Name == uniqueName);
    }

    /// <summary>
    /// Эквивалентное разбиение. Поиск по несуществующей подстроке.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_SearchByNonExistingSubstring_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/Dishes?search=ЭтогоБлюдаНет");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.DoesNotContain(dishes!, d => d.Name.Contains("ЭтогоБлюдаНет"));
    }

    /// <summary>
    /// Эквивалентное разбиение. Комбинация фильтров (категория + поиск).
    /// </summary>
    [Fact]
    public async Task GetAllDishes_CombineFilters_ReturnsFiltered()
    {
        var uniqueName = $"КомбоПоиск_{Guid.NewGuid()}";
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish(uniqueName, "Суп", productId, 200);

        // ищем супы с определённым именем
        var response = await _client.GetAsync($"/api/Dishes?category=Суп&search=КомбоПоиск");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.Contains(dishes!, d => d.Name == uniqueName && d.Category == "Суп");
    }

    #endregion

    #region Анализ граничных значений - GET /api/Dishes/{id}

    /// <summary>
    /// Граничные значения: получение блюда по существующему ID.
    /// </summary>
    [Fact]
    public async Task GetDishById_ExistingId_ReturnsOk()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish("Борщ", "Суп", productId, 200);

        var response = await _client.GetAsync($"/api/Dishes/{dishId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dish = await response.Content.ReadFromJsonAsync<DishDto>();
        Assert.NotNull(dish);
        Assert.Equal("Борщ", dish.Name);
        Assert.Equal("Суп", dish.Category);
        Assert.Single(dish.Ingredients);
        Assert.Equal(productId, dish.Ingredients[0].ProductId);
        Assert.Equal(200, dish.Ingredients[0].Quantity);
    }

    /// <summary>
    /// Граничные значения: получение блюда по несуществующему ID.
    /// </summary>
    [Fact]
    public async Task GetDishById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Dishes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Граничные значения: пустой поиск (возвращает все блюда).
    /// </summary>
    [Fact]
    public async Task GetAllDishes_EmptySearch_ReturnsAll()
    {
        var response = await _client.GetAsync("/api/Dishes?search=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.NotNull(dishes);
    }

    /// <summary>
    /// Граничные значения: поиск по одному символу.
    /// </summary>
    [Fact]
    public async Task GetAllDishes_SearchByOneCharacter_ReturnsMatching()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish("Суп", "Суп", productId, 200);

        var response = await _client.GetAsync("/api/Dishes?search=С");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<DishDto>>();
        Assert.Contains(dishes!, d => d.Name.Contains("С"));
    }

    #endregion

    #region Анализ граничных значений - Просмотр состава блюда

    /// <summary>
    /// Граничные значения: блюдо с одним ингредиентом.
    /// </summary>
    [Fact]
    public async Task GetDishById_SingleIngredient_ReturnsCorrectComposition()
    {
        var productId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var dishId = await CreateTestDish("Картофельный суп", "Суп", productId, 250);

        var response = await _client.GetAsync($"/api/Dishes/{dishId}");
        var dish = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(dish!.Ingredients);
        Assert.Equal(productId, dish.Ingredients[0].ProductId);
        Assert.Equal(250, dish.Ingredients[0].Quantity);
    }

    /// <summary>
    /// Граничные значения: блюдо с несколькими ингредиентами.
    /// </summary>
    [Fact]
    public async Task GetDishById_MultipleIngredients_ReturnsAllIngredients()
    {
        var potatoId = await CreateTestProduct("Картофель", 2, 0.4, 16.3);
        var meatId = await CreateTestProduct("Мясо", 18.9, 12.4, 0);
        var waterId = await CreateTestProduct("Вода", 0, 0, 0);

        // Создаём блюдо с тремя ингредиентами
        var createDto = new DishTestDataBuilder()
            .WithName("Борщ")
            .WithCategory("Суп")
            .WithPortionSize(500)
            .WithIngredient(potatoId, 200)
            .WithIngredient(meatId, 150)
            .WithIngredient(waterId, 400)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Dishes", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishDto>();
        _cleaner.TrackDish(created!.Id);

        var response = await _client.GetAsync($"/api/Dishes/{created.Id}");
        var dish = await response.Content.ReadFromJsonAsync<DishDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, dish!.Ingredients.Count);

        // Проверяем, что все ингредиенты на месте
        Assert.Contains(dish.Ingredients, i => i.ProductId == potatoId);
        Assert.Contains(dish.Ingredients, i => i.ProductId == meatId);
        Assert.Contains(dish.Ingredients, i => i.ProductId == waterId);
    }

    #endregion

    #region Эквивалентное разбиение - Получение несуществующего блюда

    /// <summary>
    /// Негативный тест. Эквивалентное разбиение.
    /// Класс: несуществующий ID.
    /// </summary>
    [Fact]
    public async Task GetDishById_InvalidGuidFormat_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/Dishes/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
}