using FluentAssertions;
using Xunit;
using Bimload.Core.Models;

namespace Bimload.Tests.Core.Models;

public class InstalledProgramTests
{
    [Fact]
    public void InstalledProgram_ShouldCreate_WithAllFields()
    {
        // Arrange & Act
        var program = new InstalledProgram
        {
            Name = "BIM Test Product x64",
            Version = "24.6677.6677"
        };

        // Assert
        program.Name.Should().Be("BIM Test Product x64");
        program.Version.Should().Be("24.6677.6677");
    }

    [Fact]
    public void InstalledProgram_ShouldAllowNull_ForVersion()
    {
        // Arrange & Act
        var program = new InstalledProgram
        {
            Name = "BIM Test Product x64",
            Version = null
        };

        // Assert
        program.Name.Should().Be("BIM Test Product x64");
        program.Version.Should().BeNull();
    }
}

