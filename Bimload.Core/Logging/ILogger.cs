namespace Bimload.Core.Logging;

public enum LogLevel
{
    Info,
    Error,
    Warning,
    Success
}

public interface ILogger
{
    void Log(string message, LogLevel level = LogLevel.Info);
}

