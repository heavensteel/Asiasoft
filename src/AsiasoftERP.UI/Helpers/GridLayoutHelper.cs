using AsiasoftERP.Core.Models;
using AsiasoftERP.Data.Services;
using Newtonsoft.Json;

namespace AsiasoftERP.UI.Helpers;

/// <summary>
/// UI-side helper that applies/saves grid layouts to/from DataGridView controls
/// </summary>
public static class GridLayoutHelper
{
    /// <summary>
    /// Apply saved layout to a DataGridView
    /// </summary>
    public static async Task ApplyToDataGridViewAsync(DataGridView grid, int userId, string formName)
    {
        using var db = DbContextFactory.Create();
        var service = new GridLayoutService(db);
        var columns = await service.LoadLayoutAsync(userId, formName, grid.Name);
        if (!columns.Any()) return;

        foreach (var state in columns.OrderBy(c => c.Index))
        {
            if (state.Index < grid.Columns.Count)
            {
                var col = grid.Columns[state.Index];
                col.Visible = state.Visible;
                col.Width = state.Width;
                col.DefaultCellStyle.Alignment = state.AlignmentHorz switch
                {
                    1 => DataGridViewContentAlignment.MiddleRight,
                    2 => DataGridViewContentAlignment.MiddleCenter,
                    _ => DataGridViewContentAlignment.MiddleLeft
                };
            }
        }
    }

    /// <summary>
    /// Capture current DataGridView state and save to DB
    /// </summary>
    public static async Task SaveFromDataGridViewAsync(DataGridView grid, int userId, string formName)
    {
        var columns = new List<GridColumnState>();
        foreach (DataGridViewColumn col in grid.Columns)
        {
            columns.Add(new GridColumnState
            {
                Index = col.DisplayIndex,
                FieldName = col.DataPropertyName,
                Width = col.Width,
                Visible = col.Visible,
                AlignmentHorz = col.DefaultCellStyle.Alignment switch
                {
                    DataGridViewContentAlignment.MiddleRight => 1,
                    DataGridViewContentAlignment.MiddleCenter => 2,
                    _ => 0
                }
            });
        }

        using var db = DbContextFactory.Create();
        var service = new GridLayoutService(db);
        await service.SaveLayoutAsync(userId, formName, grid.Name, columns);
    }

    /// <summary>
    /// Get column widths from JSON layout (no DataGridView needed)
    /// </summary>
    public static List<GridColumnState>? ParseLayoutJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        return JsonConvert.DeserializeObject<List<GridColumnState>>(json);
    }
}
