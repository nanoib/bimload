using Bimload.Core.Models;

namespace Bimload.Core.Services;

public class WmiService : IWmiService
{
    private readonly IWmiQueryWrapper _wmiQueryWrapper;

    public WmiService(IWmiQueryWrapper wmiQueryWrapper)
    {
        _wmiQueryWrapper = wmiQueryWrapper;
    }

    public InstalledProgram? GetLatestInstalledProgram(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        var query = $"SELECT * FROM Win32_Product WHERE Name LIKE '%{productName}%'";
        var collection = _wmiQueryWrapper.Query(query);

        var programs = new List<InstalledProgram>();

        foreach (var obj in collection)
        {
            if (!string.IsNullOrWhiteSpace(obj.Name))
            {
                programs.Add(new InstalledProgram
                {
                    Name = obj.Name,
                    Version = obj.Version
                });
            }
        }

        if (programs.Count == 0)
        {
            return null;
        }

        // Sort by version descending and return the first (latest)
        var sortedPrograms = programs
            .OrderByDescending(p => ParseVersion(p.Version))
            .ToList();

        return sortedPrograms[0];
    }

    private static Version? ParseVersion(string? versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return null;
        }

        if (Version.TryParse(versionString, out var version))
        {
            return version;
        }

        return null;
    }
}

