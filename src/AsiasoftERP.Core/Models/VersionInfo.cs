using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Application version history - tracks what changed in each version
/// </summary>
public class VersionInfo
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Version { get; set; } = string.Empty;     // e.g. "1.0.0", "1.2.3"

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;       // Version title

    [MaxLength(2000)]
    public string ReleaseNotes { get; set; } = string.Empty;  // What's new / bug fixes

    public DateTime ReleaseDate { get; set; } = DateTime.Now;

    public bool IsCritical { get; set; } = false;  // Critical security/bug fix

    [MaxLength(100)]
    public string ReleasedBy { get; set; } = string.Empty;

    // Change entries
    public ICollection<VersionChange> Changes { get; set; } = new List<VersionChange>();
}

public enum ChangeType
{
    NewFeature = 1,
    BugFix = 2,
    Improvement = 3,
    Breaking = 4,
    Security = 5
}

public class VersionChange
{
    public int Id { get; set; }

    public int VersionInfoId { get; set; }
    public VersionInfo? VersionInfo { get; set; }

    public ChangeType ChangeType { get; set; } = ChangeType.NewFeature;

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Module { get; set; }
}
