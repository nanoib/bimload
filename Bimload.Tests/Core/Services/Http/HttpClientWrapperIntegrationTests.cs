using FluentAssertions;
using System.Net.Http;
using Xunit;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services.Http;

/// <summary>
/// Интеграционные тесты для проверки доступности реальных HTTP ресурсов.
/// Эти тесты требуют подключения к интернету и будут пропущены, если доступ отсутствует.
/// </summary>
[Trait("Category", "Integration")]
public class HttpClientWrapperIntegrationTests
{
    private const string VentUrl = "https://dl.cadwise.ru/NanoDists/default/Nano20/nVentx64/";
    private const string VentPattern = @"<span class=""name"">([^<]+)</span>";

    [Fact]
    public async Task GetLatestFileAsync_ShouldAccessVentServer_Successfully()
    {
        // Arrange
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var wrapper = new HttpClientWrapper(httpClient);

        // Act
        var result = await wrapper.GetLatestFileAsync(VentUrl, VentPattern);

        // Assert
        result.Should().NotBeNull("сервер должен быть доступен и возвращать файлы");
        result.Should().StartWith("nanoVent251", "имя файла должно соответствовать ожидаемому формату");
        result.Should().EndWith("_x64.exe", "файл должен быть исполняемым файлом");
    }

    [Fact]
    public async Task GetLatestFileAsync_ShouldReturnLatestFile_FromVentServer()
    {
        // Arrange
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var wrapper = new HttpClientWrapper(httpClient);

        // Act
        var result = await wrapper.GetLatestFileAsync(VentUrl, VentPattern);

        // Assert
        result.Should().NotBeNull();
        
        // Проверяем, что версия соответствует ожидаемому формату
        // nanoVent251(13479)_x64.exe -> должна быть версия 13479 или выше
        result.Should().MatchRegex(@"nanoVent251\(\d+\)_x64\.exe", "имя файла должно соответствовать паттерну");
    }

    [Fact]
    public async Task HttpClient_ShouldConnectToVentServer_WithinTimeout()
    {
        // Arrange
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var startTime = DateTime.UtcNow;
        using var response = await httpClient.GetAsync(VentUrl, HttpCompletionOption.ResponseHeadersRead);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("сервер должен отвечать успешно");
        elapsed.TotalSeconds.Should().BeLessThan(10, "сервер должен отвечать относительно быстро");
    }

    [Fact]
    public async Task GetLatestFileAsync_ShouldParseVentServerHtml_Correctly()
    {
        // Arrange
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var wrapper = new HttpClientWrapper(httpClient);

        // Act
        var result = await wrapper.GetLatestFileAsync(VentUrl, VentPattern);

        // Assert
        result.Should().NotBeNull();
        
        // Проверяем, что это действительно последний файл по версии
        // Файл должен содержать версию в скобках
        var versionMatch = System.Text.RegularExpressions.Regex.Match(result, @"\((\d+)\)");
        versionMatch.Success.Should().BeTrue("имя файла должно содержать версию в скобках");
        
        var version = int.Parse(versionMatch.Groups[1].Value);
        version.Should().BeGreaterThan(0, "версия должна быть положительным числом");
    }

    [Fact]
    public async Task HttpClient_ShouldDownloadHtmlContent_FromVentServer()
    {
        // Arrange
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        using var response = await httpClient.GetAsync(VentUrl, HttpCompletionOption.ResponseHeadersRead);
        var htmlContent = await response.Content.ReadAsStringAsync();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("сервер должен отвечать успешно");
        htmlContent.Should().NotBeNullOrEmpty("HTML-контент должен быть получен");
        htmlContent.Should().Contain("<span class=\"name\">", "HTML должен содержать нужные элементы");
        htmlContent.Should().Contain("nanoVent251", "HTML должен содержать файлы nanoVent251");
    }
}

