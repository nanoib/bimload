using System.Runtime.Versioning;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public interface IWmiQueryWrapper
{
    IEnumerable<ManagementObjectWrapper> Query(string query);
    Task<IEnumerable<ManagementObjectWrapper>> QueryAsync(string query);
}

public class ManagementObjectWrapper
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}
