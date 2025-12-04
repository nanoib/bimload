using FluentAssertions;
using Xunit;
using Bimload.Core.Models;

namespace Bimload.Tests.Core.Models;

public class UpdateResultTests
{
    [Fact]
    public void UpdateResult_ShouldCreate_WithAllFields()
    {
        // Arrange & Act
        var result = new UpdateResult
        {
            Status = "Программа обновлена",
            OldVersion = "100",
            NewVersion = "101"
        };

        // Assert
        result.Status.Should().Be("Программа обновлена");
        result.OldVersion.Should().Be("100");
        result.NewVersion.Should().Be("101");
    }

    [Fact]
    public void UpdateResult_ShouldAllowNull_ForVersions()
    {
        // Arrange & Act
        var result = new UpdateResult
        {
            Status = "Программа не установлена",
            OldVersion = null,
            NewVersion = null
        };

        // Assert
        result.Status.Should().Be("Программа не установлена");
        result.OldVersion.Should().BeNull();
        result.NewVersion.Should().BeNull();
    }
}

