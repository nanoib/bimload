using System.Diagnostics;
using System.Runtime.Versioning;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public class ProgramInstaller : IProgramInstaller
{
    public async Task InstallProgramAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        cancellationToken.ThrowIfCancellationRequested();

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

        // Wait for process with cancellation support
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Try to kill the process if cancellation was requested
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Ignore errors when killing the process
            }
            throw;
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Installation failed with exit code: {process.ExitCode}");
        }
    }

    [SupportedOSPlatform("windows")]
    public async Task UninstallProgramAsync(InstalledProgram program, CancellationToken cancellationToken = default)
    {
        if (program == null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Use WMI to uninstall the program
        // Execute in background thread to avoid blocking UI
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            // This requires finding the program by name and calling Uninstall()
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT * FROM Win32_Product WHERE Name = '{program.Name.Replace("'", "''")}'");
            
            var collection = searcher.Get();
            foreach (System.Management.ManagementObject obj in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = obj.InvokeMethod("Uninstall", null);
                if (result != null && Convert.ToInt32(result) != 0)
                {
                    throw new InvalidOperationException($"Uninstall failed with return code: {result}");
                }
            }
        }, cancellationToken);
    }
}

