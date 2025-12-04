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
}

