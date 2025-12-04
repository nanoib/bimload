using Bimload.Core.Models;

namespace Bimload.Core.Services;

public interface IUpdateService
{
    Task<UpdateResult> UpdateAsync(Credentials credentials);
}

