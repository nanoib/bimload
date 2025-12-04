using System.Management;
using System.Runtime.Versioning;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public class WmiQueryWrapper : IWmiQueryWrapper
{
    [SupportedOSPlatform("windows")]
    public IEnumerable<ManagementObjectWrapper> Query(string query)
    {
        var searcher = new ManagementObjectSearcher(query);
        var collection = searcher.Get();

        foreach (ManagementObject obj in collection)
        {
            yield return new ManagementObjectWrapper
            {
                Name = obj["Name"]?.ToString(),
                Version = obj["Version"]?.ToString()
            };
        }
    }

    [SupportedOSPlatform("windows")]
    public async Task<IEnumerable<ManagementObjectWrapper>> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        // Execute WMI query in background thread to avoid blocking UI
        // Use ConfigureAwait(false) to prevent capturing UI synchronization context
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var searcher = new ManagementObjectSearcher(query);
            var collection = searcher.Get();

            var results = new List<ManagementObjectWrapper>();
            foreach (ManagementObject obj in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(new ManagementObjectWrapper
                {
                    Name = obj["Name"]?.ToString(),
                    Version = obj["Version"]?.ToString()
                });
            }

            return results;
        }, cancellationToken).ConfigureAwait(false);
    }
}
