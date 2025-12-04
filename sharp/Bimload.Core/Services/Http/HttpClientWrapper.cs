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

        var response = await _httpClient.GetAsync(httpUrl);
        response.EnsureSuccessStatusCode();

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

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var fileContent = await response.Content.ReadAsByteArrayAsync();
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(localFilePath, fileContent);
    }
}

