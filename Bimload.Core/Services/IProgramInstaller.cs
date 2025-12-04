using Bimload.Core.Models;

namespace Bimload.Core.Services;

public interface IProgramInstaller
{
    Task InstallProgramAsync(string filePath);
    Task UninstallProgramAsync(InstalledProgram program);
}

