using System.Net;
using System.Net.Http.Json;
using RecipeBook.IntegrationTests.Builders;
using RecipeBook.IntegrationTests.Cleaners;
using RecipeBook.IntegrationTests.Fixtures;
using RecipeBook.Core.DTOs;

namespace RecipeBook.IntegrationTests.Products;

/// <summary>
/// Интеграционные тесты для GET /api/Products.
/// Используют эквивалентное разбиение и анализ граничных значений.
/// </summary>
public class ProductsReadTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly HttpClient _client;
    private readonly DatabaseCleaner _cleaner;

    public ProductsReadTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Client;
        _cleaner = new DatabaseCleaner(_client);
    }

    public void Dispose() => _cleaner.Dispose();

    #region Эквивалентное разбиение - GET /api/Products

    /// <summary>
    /// Позитивный тест. Эквивалентное разбиение.
    /// Класс: получение списка всех продуктов.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/Products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
    }

    /// <summary>
    /// Эквивалентное разбиение. Фильтрация по существующей категории.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_FilterByExistingCategory_ReturnsFiltered()
    {
        // создаём продукт с категорией "Овощи"
        var dto = new ProductTestDataBuilder()
            .WithName($"Овощной продукт_{Guid.NewGuid()}")
            .WithCategory("Овощи")
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        var response = await _client.GetAsync("/api/Products?category=Овощи");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.All(products, p => Assert.Equal("Овощи", p.Category));
    }

    /// <summary>
    /// Эквивалентное разбиение. Фильтрация по несуществующей категории.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_FilterByNonExistingCategory_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/Products?category=НесуществующаяКатегория");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Эквивалентное разбиение. Поиск по существующей подстроке.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SearchByExistingSubstring_ReturnsFiltered()
    {
        // создаём продукт с уникальным именем
        var uniqueName = $"Поиск_{Guid.NewGuid()}";
        var dto = new ProductTestDataBuilder()
            .WithName(uniqueName)
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        var response = await _client.GetAsync($"/api/Products?search=Поиск");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Contains(products!, p => p.Name == uniqueName);
    }

    /// <summary>
    /// Эквивалентное разбиение. Поиск по несуществующей подстроке.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SearchByNonExistingSubstring_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/Products?search=ЭтогоПродуктаНет");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.DoesNotContain(products!, p => p.Name.Contains("ЭтогоПродуктаНет"));
    }

    #endregion

    #region Анализ граничных значений - GET /api/Products/{id}

    /// <summary>
    /// Граничные значения: получение продукта по существующему ID.
    /// </summary>
    [Fact]
    public async Task GetProductById_ExistingId_ReturnsOk()
    {
        // создаём продукт
        var dto = new ProductTestDataBuilder()
            .WithName($"GetById_{Guid.NewGuid()}")
            .Build();

        var createResponse = await _client.PostAsJsonAsync("/api/Products", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        _cleaner.TrackProduct(created!.Id);

        var response = await _client.GetAsync($"/api/Products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(created.Id, product!.Id);
        Assert.Equal(created.Name, product.Name);
    }

    /// <summary>
    /// Граничные значения: получение продукта по несуществующему ID.
    /// </summary>
    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Граничные значения: фильтрация по пустой строке (возвращает все продукты).
    /// </summary>
    [Fact]
    public async Task GetAllProducts_EmptySearch_ReturnsAll()
    {
        var response = await _client.GetAsync("/api/Products?search=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
    }

    #endregion

    #region Анализ граничных значений - Сортировка

    /// <summary>
    /// Граничные значения: сортировка по калориям (по возрастанию).
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SortByCalories_ReturnsSorted()
    {
        var response = await _client.GetAsync("/api/Products?sortBy=calories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        if (products != null && products.Count > 1)
        {
            var sorted = products.OrderBy(p => p.Calories).ToList();
            Assert.Equal(sorted.Select(p => p.Calories), products.Select(p => p.Calories));
        }
    }

    /// <summary>
    /// Граничные значения: сортировка по белкам.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SortByProteins_ReturnsSorted()
    {
        var response = await _client.GetAsync("/api/Products?sortBy=proteins");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        if (products != null && products.Count > 1)
        {
            var sorted = products.OrderBy(p => p.Proteins).ToList();
            Assert.Equal(sorted.Select(p => p.Proteins), products.Select(p => p.Proteins));
        }
    }

    /// <summary>
    /// Граничные значения: сортировка по жирам.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SortByFats_ReturnsSorted()
    {
        var response = await _client.GetAsync("/api/Products?sortBy=fats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        if (products != null && products.Count > 1)
        {
            var sorted = products.OrderBy(p => p.Fats).ToList();
            Assert.Equal(sorted.Select(p => p.Fats), products.Select(p => p.Fats));
        }
    }

    /// <summary>
    /// Граничные значения: сортировка по углеводам.
    /// </summary>
    [Fact]
    public async Task GetAllProducts_SortByCarbs_ReturnsSorted()
    {
        var response = await _client.GetAsync("/api/Products?sortBy=carbs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();

        if (products != null && products.Count > 1)
        {
            var sorted = products.OrderBy(p => p.Carbs).ToList();
            Assert.Equal(sorted.Select(p => p.Carbs), products.Select(p => p.Carbs));
        }
    }

    #endregion
}