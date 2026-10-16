using AsiasoftERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AsiasoftERP.Data.Services;

public class GridLayoutService
{
    private readonly AppDbContext _db;

    public GridLayoutService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<GridColumnState>> LoadLayoutAsync(int userId, string formName, string gridName)
    {
        var layout = await _db.GridLayouts
            .FirstOrDefaultAsync(g => g.UserId == userId && g.FormName == formName && g.GridName == gridName);

        if (layout == null || string.IsNullOrEmpty(layout.LayoutJson))
            return new List<GridColumnState>();

        return JsonConvert.DeserializeObject<List<GridColumnState>>(layout.LayoutJson)
               ?? new List<GridColumnState>();
    }

    public async Task SaveLayoutAsync(int userId, string formName, string gridName,
        List<GridColumnState> columns, bool filterActive = false,
        string? filterJson = null, string? sortJson = null)
    {
        var layout = await _db.GridLayouts
            .FirstOrDefaultAsync(g => g.UserId == userId && g.FormName == formName && g.GridName == gridName);

        if (layout == null)
        {
            layout = new GridLayout
            {
                UserId = userId,
                FormName = formName,
                GridName = gridName
            };
            _db.GridLayouts.Add(layout);
        }

        layout.LayoutJson = JsonConvert.SerializeObject(columns);
        layout.FilterActive = filterActive;
        layout.FilterJson = filterJson;
        layout.SortJson = sortJson;
        layout.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<GridLayout?> GetLayoutAsync(int userId, string formName, string gridName)
    {
        return await _db.GridLayouts
            .FirstOrDefaultAsync(g => g.UserId == userId && g.FormName == formName && g.GridName == gridName);
    }

    public async Task DeleteLayoutAsync(int userId, string formName)
    {
        var layouts = await _db.GridLayouts
            .Where(g => g.UserId == userId && g.FormName == formName)
            .ToListAsync();
        _db.GridLayouts.RemoveRange(layouts);
        await _db.SaveChangesAsync();
    }
}
