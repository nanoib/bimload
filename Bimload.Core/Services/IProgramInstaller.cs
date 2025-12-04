using System.Runtime.Versioning;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public interface IProgramInstaller
{
    Task InstallProgramAsync(string filePath);
    Task UninstallProgramAsync(InstalledProgram program);
}

