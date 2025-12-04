using System.Threading;
using FluentAssertions;
using Moq;
using Xunit;
using Bimload.Core.Services;

namespace Bimload.Tests.Core.Services;

public class WmiServiceTests
{
    [Fact]
    public void GetLatestInstalledProgram_ShouldReturnLatestProgram_WhenFound()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        // Now we get ALL programs and filter in code
        var allPrograms = new List<ManagementObjectWrapper>
        {
            new() { Name = "BIM Test Product x64 24", Version = "24.100.100" },
            new() { Name = "BIM Test Product x64 24", Version = "24.200.200" },
            new() { Name = "Other Product", Version = "1.0.0" }
        };

        mockWrapper.Setup(w => w.Query("SELECT * FROM Win32_Product")).Returns(allPrograms);

        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = service.GetLatestInstalledProgram("BIM Test Product");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("BIM Test Product x64 24");
        result.Version.Should().Be("24.200.200"); // Latest version
    }

    [Fact]
    public void GetLatestInstalledProgram_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var allPrograms = new List<ManagementObjectWrapper>
        {
            new() { Name = "Other Product", Version = "1.0.0" }
        };

        mockWrapper.Setup(w => w.Query("SELECT * FROM Win32_Product")).Returns(allPrograms);

        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = service.GetLatestInstalledProgram("NonExistent Product");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLatestInstalledProgram_ShouldSortByVersion_Descending()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var allPrograms = new List<ManagementObjectWrapper>
        {
            new() { Name = "BIM Test Product x64 24", Version = "24.300.300" },
            new() { Name = "BIM Test Product x64 24", Version = "24.100.100" },
            new() { Name = "BIM Test Product x64 24", Version = "24.200.200" },
            new() { Name = "Other Product", Version = "1.0.0" }
        };

        mockWrapper.Setup(w => w.Query("SELECT * FROM Win32_Product")).Returns(allPrograms);

        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = service.GetLatestInstalledProgram("BIM Test Product");

        // Assert
        result.Should().NotBeNull();
        result!.Version.Should().Be("24.300.300"); // Highest version
    }

    [Fact]
    public async Task GetLatestInstalledProgramAsync_ShouldReturnLatestProgram_WhenFound()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var allPrograms = new List<ManagementObjectWrapper>
        {
            new() { Name = "BIM Test Product x64 24", Version = "24.100.100" },
            new() { Name = "BIM Test Product x64 24", Version = "24.200.200" },
            new() { Name = "Other Product", Version = "1.0.0" }
        };

        mockWrapper.Setup(w => w.QueryAsync("SELECT * FROM Win32_Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPrograms);

        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = await service.GetLatestInstalledProgramAsync("BIM Test Product");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("BIM Test Product x64 24");
        result.Version.Should().Be("24.200.200"); // Latest version
    }

    [Fact]
    public async Task GetLatestInstalledProgramAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var allPrograms = new List<ManagementObjectWrapper>
        {
            new() { Name = "Other Product", Version = "1.0.0" }
        };

        mockWrapper.Setup(w => w.QueryAsync("SELECT * FROM Win32_Product", It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPrograms);

        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = await service.GetLatestInstalledProgramAsync("NonExistent Product");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestInstalledProgramAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockWrapper.Setup(w => w.QueryAsync("SELECT * FROM Win32_Product", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = new WmiService(mockWrapper.Object);

        // Act & Assert
        var act = async () => await service.GetLatestInstalledProgramAsync("BIM Test Product", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetLatestInstalledProgramAsync_ShouldReturnNull_WhenProductNameIsEmpty()
    {
        // Arrange
        var mockWrapper = new Mock<IWmiQueryWrapper>();
        var service = new WmiService(mockWrapper.Object);

        // Act
        var result = await service.GetLatestInstalledProgramAsync("");

        // Assert
        result.Should().BeNull();
        mockWrapper.Verify(w => w.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

