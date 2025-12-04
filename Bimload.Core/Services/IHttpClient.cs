namespace Bimload.Core.Services;

public interface IHttpClient
{
    Task<string?> GetLatestFileAsync(string httpUrl, string httpPattern);
    Task DownloadFileAsync(string url, string localFilePath);
}

