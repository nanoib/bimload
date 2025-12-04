namespace Bimload.Core.Services;

public interface IWmiQueryWrapper
{
    IEnumerable<ManagementObjectWrapper> Query(string query);
}

public class ManagementObjectWrapper
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}
