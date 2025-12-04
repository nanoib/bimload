using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Bimload.Core.Services;

public class HttpClientWrapper : IHttpClient
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public HttpClientWrapper(System.Net.Http.HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string?> GetLatestFileAsync(string httpUrl, string httpPattern)
    {
        if (string.IsNullOrWhiteSpace(httpUrl))
        {
            throw new ArgumentException("HTTP URL cannot be null or empty", nameof(httpUrl));
        }

        if (string.IsNullOrWhiteSpace(httpPattern))
        {
            throw new ArgumentException("HTTP pattern cannot be null or empty", nameof(httpPattern));
        }

        // Ensure URL ends with / if it's a directory
        var normalizedUrl = httpUrl;
        if (!normalizedUrl.EndsWith("/") && !normalizedUrl.Contains("?"))
        {
            normalizedUrl += "/";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, normalizedUrl);
        
        // Add browser-like headers to avoid blocking
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Add("Accept-Encoding", "gzip, deflate");
        request.Headers.Add("Connection", "keep-alive");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
        
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}). " +
                $"URL: {normalizedUrl}");
        }

        var htmlContent = await response.Content.ReadAsStringAsync();
        var matches = Regex.Matches(htmlContent, httpPattern, RegexOptions.IgnoreCase);

        if (matches.Count == 0)
        {
            return null;
        }

        // Return the last match (latest file)
        var lastMatch = matches[matches.Count - 1];
        if (lastMatch.Groups.Count > 1)
        {
            return lastMatch.Groups[1].Value;
        }

        return null;
    }

    public async Task DownloadFileAsync(string url, string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            throw new ArgumentException("Local file path cannot be null or empty", nameof(localFilePath));
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Use streaming download instead of loading entire file into memory
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
        
        await contentStream.CopyToAsync(fileStream);
    }
}

