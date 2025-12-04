using System.Runtime.Versioning;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public interface IWmiService
{
    InstalledProgram? GetLatestInstalledProgram(string productName);
}

