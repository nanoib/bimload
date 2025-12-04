using System.Text.RegularExpressions;
using Bimload.Core.Models;

namespace Bimload.Core.Parsers;

public class CredentialsParser : ICredentialsParser
{
    public Credentials Parse(string iniContent)
    {
        var credentials = new Credentials();

        if (string.IsNullOrWhiteSpace(iniContent))
        {
            return credentials;
        }

        var lines = iniContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip comments and empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            // Parse key=value pairs
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex <= 0)
            {
                continue;
            }

            var key = trimmedLine.Substring(0, equalIndex).Trim();
            var value = trimmedLine.Substring(equalIndex + 1).Trim();

            // Only parse HTTP-related fields, ignore FTP fields
            switch (key.ToLowerInvariant())
            {
                case "localpath":
                    credentials.LocalPath = UnescapePath(value);
                    break;
                case "productname":
                    credentials.ProductName = value;
                    break;
                case "fileversionpattern":
                    credentials.FileVersionPattern = value;
                    break;
                case "productversionpattern":
                    credentials.ProductVersionPattern = value;
                    break;
                case "httpurl":
                    credentials.HttpUrl = value;
                    break;
                case "httppattern":
                    credentials.HttpPattern = value;
                    break;
                // Ignore FTP fields: ftpUrl, ftpFolder, username, password
            }
        }

        return credentials;
    }

    private static string UnescapePath(string path)
    {
        // Convert \\ to \ for Windows paths
        return path.Replace(@"\\", @"\");
    }
}

