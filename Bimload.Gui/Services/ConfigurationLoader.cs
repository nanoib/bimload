using System.IO;
using Bimload.Core.Models;
using Bimload.Core.Parsers;

namespace Bimload.Gui.Services;

public class ConfigurationLoader
{
    private readonly ICredentialsParser _parser;

    public ConfigurationLoader(ICredentialsParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public List<ConfigurationItem> LoadConfigurations(string credsFolderPath)
    {
        var configurations = new List<ConfigurationItem>();

        if (!Directory.Exists(credsFolderPath))
        {
            return configurations;
        }

        var iniFiles = Directory.GetFiles(credsFolderPath, "*.ini");

        foreach (var iniFile in iniFiles)
        {
            try
            {
                var fileName = Path.GetFileName(iniFile);
                var content = File.ReadAllText(iniFile);
                var credentials = _parser.Parse(content);

                configurations.Add(new ConfigurationItem
                {
                    FileName = fileName,
                    Credentials = credentials
                });
            }
            catch (Exception ex)
            {
                // Log error but continue loading other files
                System.Diagnostics.Debug.WriteLine($"Error loading {iniFile}: {ex.Message}");
            }
        }

        return configurations;
    }
}

public class ConfigurationItem
{
    public string FileName { get; set; } = string.Empty;
    public Credentials Credentials { get; set; } = new();
}

