using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Stores grid column layout settings per user per form (replaces the old INI files)
/// </summary>
public class GridLayout
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(200)]
    public string FormName { get; set; } = string.Empty;   // e.g. "Tfaturalar"

    [Required, MaxLength(200)]
    public string GridName { get; set; } = string.Empty;   // e.g. "cxGrid1"

    /// <summary>
    /// JSON-serialized column settings
    /// </summary>
    public string LayoutJson { get; set; } = "[]";

    public bool FilterActive { get; set; } = false;
    public string? FilterJson { get; set; }         // Saved filter
    public string? SortJson { get; set; }           // Saved sort
    public string? GroupByJson { get; set; }        // Group-by settings

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Individual column state in a grid layout
/// </summary>
public class GridColumnState
{
    public int Index { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int Width { get; set; } = 100;
    public bool Visible { get; set; } = true;
    public string SortOrder { get; set; } = "None";  // None, Ascending, Descending
    public int SortIndex { get; set; } = -1;
    public int AlignmentHorz { get; set; } = 0;  // 0=Left, 1=Right, 2=Center
    public int GroupIndex { get; set; } = -1;
}
