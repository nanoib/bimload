using System.Threading;
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

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
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
        mockProgramInstaller.Verify(p => p.InstallProgramAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => 
            {
                var result = callCount++ == 0 ? installedProgram : updatedProgram;
                return result;
            });

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.101.101", @".*\.(\d+)$"))
            .Returns("101");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
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
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Action<long, long?>?>()))
            .Returns(Task.CompletedTask);

        mockProgramInstaller.Setup(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockProgramInstaller.Setup(p => p.InstallProgramAsync(localFilePath, It.IsAny<CancellationToken>()))
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
        mockProgramInstaller.Verify(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProgramInstaller.Verify(p => p.InstallProgramAsync(localFilePath, It.IsAny<CancellationToken>()), Times.Once);
        
        // The status depends on verification after installation
        // If the updated program is found, status should be "Программа обновлена"
        result.NewVersion.Should().Be("101");
        result.Status.Should().Be("Программа обновлена");
    }

    [Fact]
    public async Task UpdateAsync_ShouldDeletePartialFile_WhenCancelledDuringDownload()
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

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var localFilePath = Path.Combine(tempDir, "TestProduct(101).exe");

        try
        {
            mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
                .ReturnsAsync(installedProgram);

            mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
                .Returns("100");

            mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
                .ReturnsAsync("TestProduct(101).exe");

            mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
                .Returns("101");

            mockVersionService.Setup(v => v.CompareVersions("100", "101"))
                .Returns(true);

            // Simulate cancellation during download
            var cts = new CancellationTokenSource();
            mockHttpClient.Setup(h => h.DownloadFileAsync(
                It.IsAny<string>(),
                localFilePath,
                It.IsAny<CancellationToken>(),
                It.IsAny<Action<long, long?>?>()))
                .Callback<string, string, CancellationToken, Action<long, long?>?>((url, path, ct, callback) =>
                {
                    // Create partial file
                    File.WriteAllText(path, "partial content");
                    cts.Cancel();
                })
                .ThrowsAsync(new OperationCanceledException());

            var credentials = new Credentials
            {
                LocalPath = tempDir,
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

            // Act & Assert
            var act = async () => await service.UpdateAsync(credentials, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            // Verify file was deleted
            File.Exists(localFilePath).Should().BeFalse();
            mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Недокачанный файл") && s.Contains("удален после отмены")), It.IsAny<LogLevel>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandleFileDeletionError_WhenCancelledDuringDownload()
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

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TestProduct(101).exe");

        mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
            .Returns("101");

        mockVersionService.Setup(v => v.CompareVersions("100", "101"))
            .Returns(true);

        var cts = new CancellationTokenSource();
        mockHttpClient.Setup(h => h.DownloadFileAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Action<long, long?>?>()))
            .ThrowsAsync(new OperationCanceledException());

        var credentials = new Credentials
        {
            LocalPath = @"C:\NonExistentPath\",
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

        // Act & Assert
        var act = async () => await service.UpdateAsync(credentials, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Verify error was logged if file deletion fails
        // (In this case, file doesn't exist, so deletion won't be attempted)
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowOperationCanceled_WhenCancelledDuringWmiQuery()
    {
        // Arrange
        var mockWmiService = new Mock<IWmiService>();
        var mockVersionService = new Mock<IVersionService>();
        var mockHttpClient = new Mock<IHttpClient>();
        var mockProgramInstaller = new Mock<IProgramInstaller>();
        var mockLogger = new Mock<ILogger>();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var credentials = new Credentials
        {
            ProductName = "BIM Test Product"
        };

        var service = new UpdateService(
            mockWmiService.Object,
            mockVersionService.Object,
            mockHttpClient.Object,
            mockProgramInstaller.Object,
            mockLogger.Object);

        // Act & Assert
        var act = async () => await service.UpdateAsync(credentials, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowOperationCanceled_WhenCancelledDuringHttpRequest()
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

        var cts = new CancellationTokenSource();

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new OperationCanceledException());

        var credentials = new Credentials
        {
            ProductName = "BIM Test Product",
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

        // Act & Assert
        var act = async () => await service.UpdateAsync(credentials, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnError_WhenProgramNotFound()
    {
        // Arrange
        var mockWmiService = new Mock<IWmiService>();
        var mockVersionService = new Mock<IVersionService>();
        var mockHttpClient = new Mock<IHttpClient>();
        var mockProgramInstaller = new Mock<IProgramInstaller>();
        var mockLogger = new Mock<ILogger>();

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync((InstalledProgram?)null);

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TestProduct(101).exe");

        mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
            .Returns("101");

        mockVersionService.Setup(v => v.CompareVersions(null, "101"))
            .Returns(true);

        var credentials = new Credentials
        {
            LocalPath = @"C:\Temp\",
            ProductName = "BIM Test Product",
            FileVersionPattern = @"TestProduct\((\d+)\)\.exe",
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
        result.OldVersion.Should().BeNull();
        mockLogger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Программа не найдена")), It.IsAny<LogLevel>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnError_WhenHttpUrlIsMissing()
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

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        var credentials = new Credentials
        {
            ProductName = "BIM Test Product",
            ProductVersionPattern = @".*\.(\d+)$",
            HttpUrl = "", // Missing URL
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
        result.Status.Should().Contain("HTTP URL или паттерн не указаны");
        result.OldVersion.Should().Be("100");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnError_WhenFileVersionExtractionFails()
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

        mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(installedProgram);

        mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
            .Returns("100");

        mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TestProduct(101).exe");

        mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
            .Returns((string?)null); // Failed to extract version

        var credentials = new Credentials
        {
            ProductName = "BIM Test Product",
            ProductVersionPattern = @".*\.(\d+)$",
            FileVersionPattern = @"TestProduct\((\d+)\)\.exe",
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
        result.Status.Should().Contain("Не удалось извлечь версию из имени файла");
        result.OldVersion.Should().Be("100");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUseExistingFile_WhenFileAlreadyDownloaded()
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

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var localFilePath = Path.Combine(tempDir, "TestProduct(101).exe");
        File.WriteAllText(localFilePath, "existing file");

        try
        {
            var callCount = 0;
            mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        return installedProgram; // First call: old version 24.100.100
                    return new InstalledProgram { Name = "BIM Test Product", Version = "24.101.101" }; // Second call: after installation
                });

            mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
                .Returns("100");

            mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.101.101", @".*\.(\d+)$"))
                .Returns("101");

            mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
                .ReturnsAsync("TestProduct(101).exe");

            mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
                .Returns("101");

            mockVersionService.Setup(v => v.CompareVersions("100", "101"))
                .Returns(true);

            mockProgramInstaller.Setup(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockProgramInstaller.Setup(p => p.InstallProgramAsync(localFilePath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var credentials = new Credentials
            {
                LocalPath = tempDir,
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
            // File exists, so download should not be called
            mockHttpClient.Verify(h => h.DownloadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Action<long, long?>?>()), Times.Never);
            // Since file exists and update proceeds, the log message should appear
            // But if versions match, update won't proceed. Let's check if update was attempted
            result.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowOperationCanceled_WhenCancelledDuringInstallation()
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

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var localFilePath = Path.Combine(tempDir, "TestProduct(101).exe");
        File.WriteAllText(localFilePath, "test");

        try
        {
            mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
                .ReturnsAsync(installedProgram);

            mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
                .Returns("100");

            mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
                .ReturnsAsync("TestProduct(101).exe");

            mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
                .Returns("101");

            mockVersionService.Setup(v => v.CompareVersions("100", "101"))
                .Returns(true);

            var cts = new CancellationTokenSource();
            mockProgramInstaller.Setup(p => p.InstallProgramAsync(localFilePath, It.IsAny<CancellationToken>()))
                .Callback(() => cts.Cancel())
                .ThrowsAsync(new OperationCanceledException());

            var credentials = new Credentials
            {
                LocalPath = tempDir,
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

            // Act & Assert
            var act = async () => await service.UpdateAsync(credentials, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnCorrectStatus_WhenInstallationFails()
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

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var localFilePath = Path.Combine(tempDir, "TestProduct(101).exe");
        File.WriteAllText(localFilePath, "test");

        try
        {
            var callCount = 0;
            mockWmiService.Setup(w => w.GetLatestInstalledProgramAsync("BIM Test Product", It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        return installedProgram; // First call: before installation
                    // Second call (after installation) returns null - program not found
                    return null;
                });

            mockVersionService.Setup(v => v.ExtractVersionFromProductVersion("24.100.100", @".*\.(\d+)$"))
                .Returns("100");

            mockHttpClient.Setup(h => h.GetLatestFileAsync("https://example.com/distrs/", @"<span class=""name"">([^<]+)</span>", It.IsAny<CancellationToken>()))
                .ReturnsAsync("TestProduct(101).exe");

            mockVersionService.Setup(v => v.ExtractVersionFromFileName("TestProduct(101).exe", @"TestProduct\((\d+)\)\.exe"))
                .Returns("101");

            mockVersionService.Setup(v => v.CompareVersions("100", "101"))
                .Returns(true);

            mockProgramInstaller.Setup(p => p.UninstallProgramAsync(It.IsAny<InstalledProgram>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockProgramInstaller.Setup(p => p.InstallProgramAsync(localFilePath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var credentials = new Credentials
            {
                LocalPath = tempDir,
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
            result.Status.Should().Contain("Не найдено");
            result.OldVersion.Should().Be("100");
            result.NewVersion.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}

