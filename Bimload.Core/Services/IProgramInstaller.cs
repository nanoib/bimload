using System.Runtime.Versioning;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public interface IProgramInstaller
{
    Task InstallProgramAsync(string filePath, CancellationToken cancellationToken = default);
    Task UninstallProgramAsync(InstalledProgram program, CancellationToken cancellationToken = default);
}
