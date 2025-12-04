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

        try
        {
            var match = Regex.Match(productVersion, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Groups[0] is the full match, Groups[1] is the first capturing group
                if (match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                // If no capturing groups, return the full match
                return match.Value;
            }
        }
        catch (ArgumentException ex)
        {
            // Invalid regex pattern
            throw new ArgumentException($"Invalid regex pattern '{pattern}': {ex.Message}", ex);
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

