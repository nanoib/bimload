using Bimload.Gui.Forms;
using System.Runtime.InteropServices;

namespace Bimload.Gui;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Check if .NET 8.0 Runtime is available
        if (!IsDotNetRuntimeAvailable())
        {
            ShowDotNetRuntimeError();
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static bool IsDotNetRuntimeAvailable()
    {
        try
        {
            // Try to access .NET 8.0 specific features
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            
            // Check if it's .NET (not .NET Framework)
            if (!frameworkDescription.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Extract version number
            var versionMatch = System.Text.RegularExpressions.Regex.Match(
                frameworkDescription, 
                @"\.NET (\d+)\.(\d+)"
            );
            
            if (versionMatch.Success)
            {
                var majorVersion = int.Parse(versionMatch.Groups[1].Value);
                var minorVersion = int.Parse(versionMatch.Groups[2].Value);
                
                // Check if version is 8.0 or higher
                return majorVersion >= 8;
            }

            return false;
        }
        catch
        {
            // If we can't determine the version, assume it's not available
            return false;
        }
    }

    private static void ShowDotNetRuntimeError()
    {
        const string downloadUrl = "https://dotnet.microsoft.com/download/dotnet/8.0";
        const string message = 
            "Для работы приложения требуется .NET 8.0 Runtime.\n\n" +
            "Пожалуйста, установите .NET 8.0 Desktop Runtime с официального сайта Microsoft.\n\n" +
            $"Ссылка для скачивания:\n{downloadUrl}\n\n" +
            "После установки перезапустите приложение.";

        MessageBox.Show(
            message,
            "Требуется .NET 8.0 Runtime",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        // Try to open download page in browser
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = downloadUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore if browser can't be opened
        }
    }
}