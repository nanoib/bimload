using Bimload.Core.Models;

namespace Bimload.Core.Services;

public interface IWmiService
{
    InstalledProgram? GetLatestInstalledProgram(string productName);
}

