using FluentAssertions;
using Xunit;
using Bimload.Core.Logging;

namespace Bimload.Tests.Core.Logging;

public class LoggerTests
{
    [Fact]
    public void Log_ShouldFormatMessage_WithTimestamp()
    {
        // Arrange
        var logger = new TestLogger();
        var message = "Test message";

        // Act
        logger.Log(message, LogLevel.Info);

        // Assert
        logger.LoggedMessages.Should().HaveCount(1);
        logger.LoggedMessages[0].Should().Contain(message);
        logger.LoggedMessages[0].Should().MatchRegex(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]");
    }

    [Fact]
    public void Log_ShouldLogDifferentLevels()
    {
        // Arrange
        var logger = new TestLogger();

        // Act
        logger.Log("Info message", LogLevel.Info);
        logger.Log("Error message", LogLevel.Error);
        logger.Log("Warning message", LogLevel.Warning);
        logger.Log("Success message", LogLevel.Success);

        // Assert
        logger.LoggedMessages.Should().HaveCount(4);
        logger.LoggedLevels.Should().Contain(LogLevel.Info);
        logger.LoggedLevels.Should().Contain(LogLevel.Error);
        logger.LoggedLevels.Should().Contain(LogLevel.Warning);
        logger.LoggedLevels.Should().Contain(LogLevel.Success);
    }

    [Fact]
    public void Log_ShouldUseInfoLevel_ByDefault()
    {
        // Arrange
        var logger = new TestLogger();

        // Act
        logger.Log("Test message");

        // Assert
        logger.LoggedLevels.Should().Contain(LogLevel.Info);
    }
}

// Test implementation of ILogger for testing
public class TestLogger : ILogger
{
    public List<string> LoggedMessages { get; } = new();
    public List<LogLevel> LoggedLevels { get; } = new();

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LoggedMessages.Add($"[{timestamp}] {message}");
        LoggedLevels.Add(level);
    }
}

