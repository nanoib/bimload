using FluentAssertions;
using Xunit;
using Bimload.Core.Models;

namespace Bimload.Tests.Core.Models;

public class CredentialsTests
{
    [Fact]
    public void Credentials_ShouldCreate_WithAllHttpFields()
    {
        // Arrange & Act
        var credentials = new Credentials
        {
            LocalPath = @"C:\Users\test\Desktop\Distr\",
            ProductName = "BIM Test Product",
            FileVersionPattern = @"Test(\d+)\.exe",
            ProductVersionPattern = @".*\.(\d+)$",
            HttpUrl = "https://example.com/distrs/",
            HttpPattern = @"<span class=""name"">([^<]+)</span>"
        };

        // Assert
        credentials.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        credentials.ProductName.Should().Be("BIM Test Product");
        credentials.FileVersionPattern.Should().Be(@"Test(\d+)\.exe");
        credentials.ProductVersionPattern.Should().Be(@".*\.(\d+)$");
        credentials.HttpUrl.Should().Be("https://example.com/distrs/");
        credentials.HttpPattern.Should().Be(@"<span class=""name"">([^<]+)</span>");
    }

    [Fact]
    public void Credentials_ShouldAllowNull_ForOptionalFields()
    {
        // Arrange & Act
        var credentials = new Credentials
        {
            LocalPath = @"C:\Users\test\Desktop\Distr\",
            ProductName = "BIM Test Product"
        };

        // Assert
        credentials.FileVersionPattern.Should().BeNull();
        credentials.ProductVersionPattern.Should().BeNull();
        credentials.HttpUrl.Should().BeNull();
        credentials.HttpPattern.Should().BeNull();
    }
}

