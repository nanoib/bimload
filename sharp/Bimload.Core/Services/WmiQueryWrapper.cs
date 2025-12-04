using System.Management;

namespace Bimload.Core.Services;

public class WmiQueryWrapper : IWmiQueryWrapper
{
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
