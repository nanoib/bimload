using System.Text.RegularExpressions;

namespace Bimload.Core.Services;

public class VersionService : IVersionService
{
    public string? ExtractVersionFromFileName(string fileName, string pattern)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    public string? ExtractVersionFromProductVersion(string productVersion, string pattern)
    {
        if (string.IsNullOrWhiteSpace(productVersion) || string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        var match = Regex.Match(productVersion, pattern, RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    public bool CompareVersions(string? oldVersion, string? newVersion)
    {
        // If old version is null or empty, update is needed
        if (string.IsNullOrWhiteSpace(oldVersion))
        {
            return !string.IsNullOrWhiteSpace(newVersion);
        }

        // If new version is null or empty, no update needed
        if (string.IsNullOrWhiteSpace(newVersion))
        {
            return false;
        }

        // Try to parse as integers
        if (int.TryParse(oldVersion, out var oldInt) && int.TryParse(newVersion, out var newInt))
        {
            return oldInt < newInt;
        }

        // If parsing fails, compare as strings
        return string.Compare(oldVersion, newVersion, StringComparison.Ordinal) < 0;
    }
}

