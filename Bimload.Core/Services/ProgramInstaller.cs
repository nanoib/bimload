using System.Diagnostics;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

public class ProgramInstaller : IProgramInstaller
{
    public async Task InstallProgramAsync(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Installation file not found", filePath);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = filePath,
            Arguments = "/quiet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start installation process");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Installation failed with exit code: {process.ExitCode}");
        }
    }

    public async Task UninstallProgramAsync(InstalledProgram program)
    {
        if (program == null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        // Use WMI to uninstall the program
        // This requires finding the program by name and calling Uninstall()
        using var searcher = new System.Management.ManagementObjectSearcher(
            $"SELECT * FROM Win32_Product WHERE Name = '{program.Name.Replace("'", "''")}'");
        
        var collection = searcher.Get();
        foreach (System.Management.ManagementObject obj in collection)
        {
            var result = obj.InvokeMethod("Uninstall", null);
            if (result != null && Convert.ToInt32(result) != 0)
            {
                throw new InvalidOperationException($"Uninstall failed with return code: {result}");
            }
        }

        await Task.CompletedTask;
    }
}

