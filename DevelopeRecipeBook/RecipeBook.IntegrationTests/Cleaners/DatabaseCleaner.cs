using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace RecipeBook.IntegrationTests.Cleaners;

/// <summary>
/// Очистка тестовых данных из БД после тестов.
/// </summary>
public class DatabaseCleaner : IDisposable
{
    private readonly HttpClient _client;
    private readonly List<Guid> _createdProductIds = new();
    private readonly List<Guid> _createdDishIds = new();

    public DatabaseCleaner(HttpClient client)
    {
        _client = client;
    }

    public void TrackProduct(Guid id) => _createdProductIds.Add(id);
    public void TrackDish(Guid id) => _createdDishIds.Add(id);

    public async Task CleanupAsync()
    {
        foreach (var id in _createdDishIds)
        {
            try
            {
                await _client.DeleteAsync($"/api/Dishes/{id}");
            }
            catch { /* Подавляем ошибки при удалении */ }
        }

        foreach (var id in _createdProductIds)
        {
            try
            {
                await _client.DeleteAsync($"/api/Products/{id}");
            }
            catch { /* Подавляем ошибки при удалении */ }
        }
    }

    public void Dispose()
    {
        CleanupAsync().GetAwaiter().GetResult();
    }
}