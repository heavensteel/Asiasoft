using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, VIEW, LOGIN, LOGOUT

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string? OldValues { get; set; }   // JSON
    public string? NewValues { get; set; }   // JSON

    [MaxLength(100)]
    public string MachineName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
