using AsiasoftERP.Core.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AsiasoftERP.Data.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool success, User? user, string message)> LoginAsync(
        string username, string password, string machineName, string sessionName, int? rdpSessionId = null)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, null, "Kullanıcı adı veya şifre hatalı.");

        // Update last login info
        user.LastLogin = DateTime.Now;
        user.LastLoginMachine = machineName;
        user.LastLoginSession = sessionName;

        // Create session record
        var session = new UserSession
        {
            UserId = user.Id,
            MachineName = machineName,
            SessionName = sessionName,
            RdpSessionId = rdpSessionId,
            LoginTime = DateTime.Now,
            IsActive = true
        };
        _db.UserSessions.Add(session);

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Module = "Auth",
            Action = "LOGIN",
            Description = $"Kullanıcı {username} giriş yaptı. Makine: {machineName}, Oturum: {sessionName}",
            MachineName = machineName
        });

        await _db.SaveChangesAsync();
        return (true, user, "Giriş başarılı.");
    }

    public async Task LogoutAsync(int userId, string machineName)
    {
        var session = await _db.UserSessions
            .Where(s => s.UserId == userId && s.IsActive && s.MachineName == machineName)
            .OrderByDescending(s => s.LoginTime)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.IsActive = false;
            session.LogoutTime = DateTime.Now;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Module = "Auth",
            Action = "LOGOUT",
            Description = $"Kullanıcı çıkış yaptı. Makine: {machineName}",
            MachineName = machineName
        });

        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByTokenAsync(string token)
    {
        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.RememberMeToken == token &&
                u.TokenExpiry.HasValue &&
                u.TokenExpiry.Value > DateTime.Now &&
                u.IsActive);
    }

    public async Task<string> SetRememberMeTokenAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return string.Empty;

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                    Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        user.RememberMeToken = token;
        user.RememberMe = true;
        user.TokenExpiry = DateTime.Now.AddDays(30);
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task ClearRememberMeTokenAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        user.RememberMeToken = null;
        user.RememberMe = false;
        user.TokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    public async Task<User?> CreateUserAsync(string username, string password, string fullName, int roleId)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username))
            return null;

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            RoleId = roleId,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserSession>> GetActiveSessionsAsync()
    {
        return await _db.UserSessions
            .Include(s => s.User)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync();
    }
}
