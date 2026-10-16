using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(100)]
    public string MachineName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SessionName { get; set; } = string.Empty;

    public int? RdpSessionId { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.Now;
    public DateTime? LogoutTime { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(200)]
    public string? CurrentModule { get; set; }
}
