using FluentAssertions;
using Moq;
using Xunit;
using Bimload.Core.Models;
using Bimload.Core.Logging;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services;

public class UpdateServiceTests
{
    [Fact]
    public async Task UpdateAsync_ShouldSkipUpdate_WhenVersionIsUpToDate()
    {
        // Arrange
        var mockWmiService = new Mock<IWmiService>();
        var mockVersionService = new Mock<IVersionService>();
        var mockHttpClient = new Mock<IHttpClient>();
        var mockProgramInstaller = new Mock<IProgramInstaller>();
        var mockLogger = new Mock<ILogger>();

        var installedProgram = new InstalledProgram
        {
            Name = "BIM Test Product",
            Version = "24.100.100"
        };

        mockWmiService.Setup(w => w.GetLatestInstalledProgram("BIM Test Product"))
            .Returns(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>"))
            .ReturnsAsync("TestProduct(100).exe");

        mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(100).exe", @"TestProduct\((\d+)\)\.exe"))
            .Returns("100");

        mockVersionService.Setup(v => v.CompareVersions("100", "100"))
            .Returns(false);

        var credentials = new Credentials
        {
            LocalPath = @"C:\Temp\",
            ProductName = "BIM Test Product",
            FileVersionPattern = @"TestProduct\((\d+)\)\.exe",
            ProductVersionPattern = @".*\.(\d+)$",
            HttpUrl = "https://example.com/distrs/",
            HttpPattern = @"<span class=""name"">([^<]+)</span>"
        };

        var service = new UpdateService(
            mockWmiService.Object,
            mockVersionService.Object,
            mockHttpClient.Object,
            mockProgramInstaller.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateAsync(credentials);

        // Assert
        result.Status.Should().Be("Обновление не требуется");
        result.OldVersion.Should().Be("100");
        result.NewVersion.Should().Be("100");
        mockProgramInstaller.Verify(p => p.InstallProgramAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdate_WhenNewVersionAvailable()
    {
        // Arrange
        var mockWmiService = new Mock<IWmiService>();
        var mockVersionService = new Mock<IVersionService>();
        var mockHttpClient = new Mock<IHttpClient>();
        var mockProgramInstaller = new Mock<IProgramInstaller>();
        var mockLogger = new Mock<ILogger>();

        var installedProgram = new InstalledProgram
        {
            Name = "BIM Test Product",
            Version = "24.100.100"
        };

        var updatedProgram = new InstalledProgram
        {
            Name = "BIM Test Product",
            Version = "24.101.101"
        };

        // Setup: first call returns old version, subsequent calls return new version
        var callCount = 0;
        mockWmiService.Setup(w => w.GetLatestInstalledProgram("BIM Test Product"))
            .Returns(() => callCount++ == 0 ? installedProgram : updatedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.101.101", @".*\.(\d+)$"))
            .Returns("101");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>"))
            .ReturnsAsync("TestProduct(101).exe");

        mockVersionService.Setup(v => v.ExtractVersionFromFileName(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((fileName, pattern) =>
            {
                if (fileName == "TestProduct(101).exe" && pattern == @"TestProduct\((\d+)\)\.exe")
                    return "101";
                return null;
            });

        mockVersionService.Setup(v => v.CompareVersions(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns<string?, string?>((old, @new) => 
            {
                if (old == "100" && @new == "101") return true;
                return false;
            });

        var localFilePath = Path.Combine(@"C:\Temp\", "TestProduct(101).exe");
        mockHttpClient.Setup(h => h.DownloadFileAsync(
            It.IsAny<string>(),
            localFilePath))
            .Returns(Task.CompletedTask);

        mockProgramInstaller.Setup(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>()))
            .Returns(Task.CompletedTask);

        mockProgramInstaller.Setup(p => p.InstallProgramAsync(localFilePath))
            .Returns(Task.CompletedTask);

        var credentials = new Credentials
        {
            LocalPath = @"C:\Temp\",
            ProductName = "BIM Test Product",
            FileVersionPattern = @"TestProduct\((\d+)\)\.exe",
            ProductVersionPattern = @".*\.(\d+)$",
            HttpUrl = "https://example.com/distrs/",
            HttpPattern = @"<span class=""name"">([^<]+)</span>"
        };

        var service = new UpdateService(
            mockWmiService.Object,
            mockVersionService.Object,
            mockHttpClient.Object,
            mockProgramInstaller.Object,
            mockLogger.Object);

        // Act
        var result = await service.UpdateAsync(credentials);

        // Assert
        result.OldVersion.Should().Be("100");
        // Verify that update was attempted
        mockProgramInstaller.Verify(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>()), Times.Once);
        mockProgramInstaller.Verify(p => p.InstallProgramAsync(localFilePath), Times.Once);
        
        // The status depends on verification after installation
        // If the updated program is found, status should be "Программа обновлена"
        result.NewVersion.Should().Be("101");
        result.Status.Should().Be("Программа обновлена");
    }
}

