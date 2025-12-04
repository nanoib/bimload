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
}
