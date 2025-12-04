using FluentAssertions;
using Moq;
using System.Diagnostics;
using Xunit;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services;

public class ProgramInstallerTests
{
    // Note: Testing Process execution is complex and typically requires integration tests
    // For unit tests, we'll focus on testing the logic around process execution
    // Actual process execution will be tested in integration tests

    [Fact]
    public async Task InstallProgram_ShouldThrowException_WhenFilePathIsNull()
    {
        // Arrange
        var installer = new ProgramInstaller();

        // Act & Assert
        var act = async () => await installer.InstallProgramAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InstallProgram_ShouldThrowException_WhenFilePathIsEmpty()
    {
        // Arrange
        var installer = new ProgramInstaller();

        // Act & Assert
        var act = async () => await installer.InstallProgramAsync(string.Empty);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UninstallProgram_ShouldThrowException_WhenProgramIsNull()
    {
        // Arrange
        var installer = new ProgramInstaller();

        // Act & Assert
        var act = async () => await installer.UninstallProgramAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

