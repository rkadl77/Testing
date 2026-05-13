using System;
using System.Net.Http;
using Xunit;

namespace RecipeBook.IntegrationTests.Fixtures;

/// <summary>
/// Фикстура для интеграционных тестов.
/// Выполняется один раз для всех тестов.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public HttpClient Client { get; }
    private readonly string _serverUrl;

    public IntegrationTestFixture()
    {
        _serverUrl = "http://localhost:5187";
        Client = new HttpClient();
        Client.BaseAddress = new Uri(_serverUrl);
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}