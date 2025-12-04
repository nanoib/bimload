using FluentAssertions;
using Xunit;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services;

public class VersionServiceTests
{
    private readonly IVersionService _versionService = new VersionService();

    [Fact]
    public void ExtractVersionFromFileName_ShouldExtractVersion_UsingRegex()
    {
        // Arrange - pattern from .ini file: NCOps241\\((\\d+)\\)_x64\\.exe
        // After parsing, it becomes: NCOps241\((\d+)\)_x64\.exe
        var fileName = "NCOps241(6677)_x64.exe";
        var pattern = @"NCOps241\((\d+)\)_x64\.exe";

        // Act
        var result = _versionService.ExtractVersionFromFileName(fileName, pattern);

        // Assert
        result.Should().Be("6677");
    }

    [Fact]
    public void ExtractVersionFromProductVersion_ShouldExtractVersion_UsingRegex()
    {
        // Arrange - pattern from .ini file: .*\\.(\\d+)$
        // After parsing, it becomes: .*\.(\d+)$
        var productVersion = "24.6677.6677";
        var pattern = @".*\.(\d+)$";

        // Act
        var result = _versionService.ExtractVersionFromProductVersion(productVersion, pattern);

        // Assert
        result.Should().Be("6677");
    }

    [Fact]
    public void CompareVersions_ShouldReturnTrue_WhenOldVersionIsLessThanNew()
    {
        // Arrange
        var oldVersion = "100";
        var newVersion = "101";

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompareVersions_ShouldReturnFalse_WhenVersionsAreEqual()
    {
        // Arrange
        var oldVersion = "100";
        var newVersion = "100";

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareVersions_ShouldReturnFalse_WhenOldVersionIsGreaterThanNew()
    {
        // Arrange
        var oldVersion = "101";
        var newVersion = "100";

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareVersions_ShouldReturnTrue_WhenOldVersionIsNull()
    {
        // Arrange
        string? oldVersion = null;
        var newVersion = "100";

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompareVersions_ShouldReturnTrue_WhenOldVersionIsEmpty()
    {
        // Arrange
        var oldVersion = string.Empty;
        var newVersion = "100";

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CompareVersions_ShouldReturnFalse_WhenNewVersionIsNull()
    {
        // Arrange
        var oldVersion = "100";
        string? newVersion = null;

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareVersions_ShouldReturnFalse_WhenBothVersionsAreNull()
    {
        // Arrange
        string? oldVersion = null;
        string? newVersion = null;

        // Act
        var result = _versionService.CompareVersions(oldVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }
}

