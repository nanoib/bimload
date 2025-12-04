namespace Bimload.Core.Models;

public class Credentials
{
    public string? LocalPath { get; set; }
    public string? ProductName { get; set; }
    public string? FileVersionPattern { get; set; }
    public string? ProductVersionPattern { get; set; }
    public string? HttpUrl { get; set; }
    public string? HttpPattern { get; set; }
}

