using System.Runtime.Versioning;
using Bimload.Core.Logging;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public class WmiService : IWmiService
{
    private readonly IWmiQueryWrapper _wmiQueryWrapper;
    private readonly ILogger? _logger;

    public WmiService(IWmiQueryWrapper wmiQueryWrapper, ILogger? logger = null)
    {
        _wmiQueryWrapper = wmiQueryWrapper;
        _logger = logger;
    }

    public InstalledProgram? GetLatestInstalledProgram(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        // Get all products and filter in code (like PowerShell does)
        // This approach is more reliable with Unicode characters and special characters
        var query = "SELECT * FROM Win32_Product";
        
        _logger?.Log($"Получаем список всех установленных программ", LogLevel.Info);
        
        var collection = _wmiQueryWrapper.Query(query);

        var programs = new List<InstalledProgram>();

        // Filter programs by name (case-insensitive, contains match)
        foreach (var obj in collection)
        {
            if (!string.IsNullOrWhiteSpace(obj.Name))
            {
                // Use case-insensitive comparison like PowerShell -like operator
                if (obj.Name.Contains(productName, StringComparison.OrdinalIgnoreCase))
                {
                    programs.Add(new InstalledProgram
                    {
                        Name = obj.Name,
                        Version = obj.Version
                    });
                }
            }
        }

        if (programs.Count == 0)
        {
            _logger?.Log($"Не найдено программ по запросу '{productName}'", LogLevel.Warning);
            return null;
        }

        _logger?.Log($"Найдено программ: {programs.Count}", LogLevel.Info);
        foreach (var program in programs)
        {
            _logger?.Log($"  - {program.Name}, Версия: {program.Version}", LogLevel.Info);
        }

        // Sort by version descending and return the first (latest)
        var sortedPrograms = programs
            .OrderByDescending(p => ParseVersion(p.Version))
            .ToList();

        _logger?.Log($"Последняя установленная сборка определена: {sortedPrograms[0].Version}", LogLevel.Info);

        return sortedPrograms[0];
    }

    public async Task<InstalledProgram?> GetLatestInstalledProgramAsync(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }

        // Get all products and filter in code (like PowerShell does)
        // This approach is more reliable with Unicode characters and special characters
        var query = "SELECT * FROM Win32_Product";
        
        _logger?.Log($"Получаем список всех установленных программ", LogLevel.Info);
        
        // Use ConfigureAwait(false) to prevent blocking UI thread
        var collection = await _wmiQueryWrapper.QueryAsync(query).ConfigureAwait(false);

        var programs = new List<InstalledProgram>();

        // Filter programs by name (case-insensitive, contains match)
        foreach (var obj in collection)
        {
            if (!string.IsNullOrWhiteSpace(obj.Name))
            {
                // Use case-insensitive comparison like PowerShell -like operator
                if (obj.Name.Contains(productName, StringComparison.OrdinalIgnoreCase))
                {
                    programs.Add(new InstalledProgram
                    {
                        Name = obj.Name,
                        Version = obj.Version
                    });
                }
            }
        }

        if (programs.Count == 0)
        {
            _logger?.Log($"Не найдено программ по запросу '{productName}'", LogLevel.Warning);
            return null;
        }

        _logger?.Log($"Найдено программ: {programs.Count}", LogLevel.Info);
        foreach (var program in programs)
        {
            _logger?.Log($"  - {program.Name}, Версия: {program.Version}", LogLevel.Info);
        }

        // Sort by version descending and return the first (latest)
        var sortedPrograms = programs
            .OrderByDescending(p => ParseVersion(p.Version))
            .ToList();

        _logger?.Log($"Последняя установленная сборка определена: {sortedPrograms[0].Version}", LogLevel.Info);

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

