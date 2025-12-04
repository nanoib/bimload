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

    [Fact]
    public async Task InstallProgramAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        var installer = new ProgramInstaller();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Note: We can't easily test actual process cancellation without a real executable
        // This test verifies that cancellation token is checked
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".exe");
        File.WriteAllText(tempFile, "fake exe");

        try
        {
            // Act & Assert
            var act = async () => await installer.InstallProgramAsync(tempFile, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task UninstallProgramAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        var installer = new ProgramInstaller();
        var program = new Bimload.Core.Models.InstalledProgram
        {
            Name = "Test Program",
            Version = "1.0.0"
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await installer.UninstallProgramAsync(program, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

