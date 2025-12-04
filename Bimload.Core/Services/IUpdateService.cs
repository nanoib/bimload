using System.Runtime.Versioning;
using Bimload.Core.Models;

namespace Bimload.Core.Services;

[SupportedOSPlatform("windows")]
public interface IUpdateService
{
    Task<UpdateResult> UpdateAsync(Credentials credentials, CancellationToken cancellationToken = default, Action<long, long?>? downloadProgressCallback = null);
}
