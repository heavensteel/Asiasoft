using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

public class EtilenDatabaseRoot
{
    public List<EtilenRoomDto> Rooms { get; set; } = new();
    public List<EtilenLogDto> Logs { get; set; } = new();
}

public class EtilenRoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Grp { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsSaha { get; set; }
    public int SortOrder { get; set; }
    public bool Active { get; set; } = true;
    public bool EthyleneActive { get; set; }
    public List<EtilenLotDto> Lots { get; set; } = new();

    public int TotalPallets
    {
        get
        {
            int sum = 0;
            if (Lots != null)
            {
                foreach (var lot in Lots) sum += lot.Pallets;
            }
            return sum;
        }
    }

    public double OccupancyPercent => Capacity > 0 && !IsSaha
        ? Math.Min(100.0, Math.Round((double)TotalPallets / Capacity * 100.0, 1))
        : 0.0;
}

public class EtilenLotDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Grower { get; set; } = string.Empty;
    public string Fruit { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public string HarvestDate { get; set; } = string.Empty;
    public long StartTime { get; set; }
    public int Pallets { get; set; }
    public double DurationHours { get; set; }
    public long UpdatedAt { get; set; }
    public long? MovedToSahaTime { get; set; }

    public DateTime? StartDateTime => StartTime > 0
        ? DateTimeOffset.FromUnixTimeMilliseconds(StartTime).LocalDateTime
        : null;

    public double HoursRemaining
    {
        get
        {
            if (StartTime <= 0 || DurationHours <= 0) return 0;
            var start = DateTimeOffset.FromUnixTimeMilliseconds(StartTime).LocalDateTime;
            var end = start.AddHours(DurationHours);
            var remaining = (end - DateTime.Now).TotalHours;
            return remaining > 0 ? Math.Round(remaining, 1) : 0;
        }
    }
}

public class EtilenLogDto
{
    public long Id { get; set; }
    public long Timestamp { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Grower { get; set; } = string.Empty;
    public string Fruit { get; set; } = string.Empty;
    public string Variety { get; set; } = string.Empty;
    public int? Pallets { get; set; }
    public string Action { get; set; } = string.Empty;
    public string StatusStr { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public string OsUser { get; set; } = string.Empty;

    public DateTime DateTime => Timestamp > 0
        ? DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).LocalDateTime
        : DateTime.Now;
}
