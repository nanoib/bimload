using Bimload.Core.Models;

namespace Bimload.Core.Parsers;

public interface ICredentialsParser
{
    Credentials Parse(string iniContent);
}

