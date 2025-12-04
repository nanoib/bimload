namespace Bimload.Core.Services;

public interface IHttpClient
{
    Task<string?> GetLatestFileAsync(string httpUrl, string httpPattern, CancellationToken cancellationToken = default);
    Task DownloadFileAsync(string url, string localFilePath, CancellationToken cancellationToken = default, Action<long, long?>? progressCallback = null);
}

