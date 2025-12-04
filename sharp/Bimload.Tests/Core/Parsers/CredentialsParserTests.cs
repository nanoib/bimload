using FluentAssertions;
using Xunit;
using Bimload.Core.Models;
using Bimload.Core.Parsers;

namespace Bimload.Tests.Core.Parsers;

public class CredentialsParserTests
{
    private readonly ICredentialsParser _parser = new CredentialsParser();

    [Fact]
    public void Parse_ShouldParseValidIniFile_WithHttpFields()
    {
        // Arrange
        var iniContent = @"localPath=C:\\Users\\test\\Desktop\\Distr\\
productName=BIM Test Product
fileVersionPattern=Test(\d+)\.exe
productVersionPattern=.*\.(\d+)$
httpUrl=https://example.com/distrs/
httpPattern=<span class=""name"">([^<]+)</span>";

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        result.ProductName.Should().Be("BIM Test Product");
        result.FileVersionPattern.Should().Be(@"Test(\d+)\.exe");
        result.ProductVersionPattern.Should().Be(@".*\.(\d+)$");
        result.HttpUrl.Should().Be("https://example.com/distrs/");
        result.HttpPattern.Should().Be(@"<span class=""name"">([^<]+)</span>");
    }

    [Fact]
    public void Parse_ShouldIgnoreComments_LinesStartingWithHash()
    {
        // Arrange
        var iniContent = @"# This is a comment
localPath=C:\\Users\\test\\Desktop\\Distr\\
# Another comment
productName=BIM Test Product
# Comment at the end";

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        result.ProductName.Should().Be("BIM Test Product");
    }

    [Fact]
    public void Parse_ShouldHandleMissingFields_ReturnNullForMissingFields()
    {
        // Arrange
        var iniContent = @"localPath=C:\\Users\\test\\Desktop\\Distr\\
productName=BIM Test Product";

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        result.ProductName.Should().Be("BIM Test Product");
        result.FileVersionPattern.Should().BeNull();
        result.ProductVersionPattern.Should().BeNull();
        result.HttpUrl.Should().BeNull();
        result.HttpPattern.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldHandleEmptyFile_ReturnEmptyCredentials()
    {
        // Arrange
        var iniContent = string.Empty;

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().BeNull();
        result.ProductName.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldIgnoreFtpFields_IfPresentInOldFiles()
    {
        // Arrange
        var iniContent = @"localPath=C:\\Users\\test\\Desktop\\Distr\\
productName=BIM Test Product
httpUrl=https://example.com/distrs/
httpPattern=<span class=""name"">([^<]+)</span>
ftpUrl=ftp://1.2.3.4:2121
ftpFolder=some/folder/
username=testuser
password=testpass";

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        result.ProductName.Should().Be("BIM Test Product");
        result.HttpUrl.Should().Be("https://example.com/distrs/");
        result.HttpPattern.Should().Be(@"<span class=""name"">([^<]+)</span>");
        // FTP fields should be ignored (not in Credentials model)
    }

    [Fact]
    public void Parse_ShouldHandleWhitespace_TrimValues()
    {
        // Arrange
        var iniContent = @"localPath=  C:\\Users\\test\\Desktop\\Distr\\  
productName=  BIM Test Product  ";

        // Act
        var result = _parser.Parse(iniContent);

        // Assert
        result.Should().NotBeNull();
        result.LocalPath.Should().Be(@"C:\Users\test\Desktop\Distr\");
        result.ProductName.Should().Be("BIM Test Product");
    }
}

