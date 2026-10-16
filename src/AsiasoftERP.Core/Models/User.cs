using System.ComponentModel.DataAnnotations;

namespace AsiasoftERP.Core.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;
    public bool RememberMe { get; set; } = false;

    [MaxLength(512)]
    public string? RememberMeToken { get; set; }

    public DateTime? TokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }
    public string? LastLoginMachine { get; set; }
    public string? LastLoginSession { get; set; }

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
