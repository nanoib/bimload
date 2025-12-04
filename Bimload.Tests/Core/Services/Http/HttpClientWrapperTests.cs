using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services.Http;

public class HttpClientWrapperTests
{
    [Fact]
    public async Task GetLatestFileAsync_ShouldReturnLatestFile_WhenFound()
    {
        // Arrange
        var htmlContent = @"<html>
<body>
<span class=""name"">file1.exe</span>
<span class=""name"">file2.exe</span>
<span class=""name"">file3.exe</span>
</body>
</html>";

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);

        // Act
        var result = await wrapper.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>");

        // Assert
        result.Should().Be("file3.exe"); // Last match
    }

    [Fact]
    public async Task GetLatestFileAsync_ShouldReturnNull_WhenNoMatches()
    {
        // Arrange
        var htmlContent = @"<html><body>No files here</body></html>";

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlContent, Encoding.UTF8, "text/html")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);

        // Act
        var result = await wrapper.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldDownloadFile_Successfully()
    {
        // Arrange
        var fileContent = "Test file content";
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fileContent, Encoding.UTF8, "application/octet-stream")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");

        try
        {
            // Act
            await wrapper.DownloadFileAsync("https://example.com/file.exe", tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Be(fileContent);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GetLatestFileAsync_ShouldHandleHttpErrors()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);

        // Act & Assert
        var act = async () => await wrapper.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>");
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetLatestFileAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await wrapper.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("test", Encoding.UTF8, "application/octet-stream")
                });
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            var act = async () => await wrapper.DownloadFileAsync("https://example.com/file.exe", tempFile, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task DownloadFileAsync_ShouldCallProgressCallback()
    {
        // Arrange
        var fileContent = new string('a', 16384); // 16KB
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(fileContent, Encoding.UTF8, "application/octet-stream")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var wrapper = new HttpClientWrapper(httpClient);
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");
        var progressCallbackCalled = false;
        long? totalBytes = null;
        long downloadedBytes = 0;

        Action<long, long?> progressCallback = (downloaded, total) =>
        {
            progressCallbackCalled = true;
            downloadedBytes = downloaded;
            totalBytes = total;
        };

        try
        {
            // Act
            await wrapper.DownloadFileAsync("https://example.com/file.exe", tempFile, progressCallback: progressCallback);

            // Assert
            progressCallbackCalled.Should().BeTrue();
            downloadedBytes.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

