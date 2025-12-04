namespace Bimload.Core.Models;

public class UpdateResult
{
    public string Status { get; set; } = string.Empty;
    public string? OldVersion { get; set; }
    public string? NewVersion { get; set; }
}

