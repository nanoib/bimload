namespace Bimload.Core.Services;

public interface IVersionService
{
    string? ExtractVersionFromFileName(string fileName, string pattern);
    string? ExtractVersionFromProductVersion(string productVersion, string pattern);
    bool CompareVersions(string? oldVersion, string? newVersion);
}

