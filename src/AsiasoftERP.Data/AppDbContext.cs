using AsiasoftERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AsiasoftERP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ERP
    public DbSet<CariHesap> CariHesaplar => Set<CariHesap>();
    public DbSet<CariEkstrem> CariEkstreler => Set<CariEkstrem>();
    public DbSet<Fatura> Faturalar => Set<Fatura>();
    public DbSet<FaturaDetay> FaturaDetaylar => Set<FaturaDetay>();
    public DbSet<Stok> Stoklar => Set<Stok>();
    public DbSet<StokHareket> StokHareketler => Set<StokHareket>();
    public DbSet<OdemeTalebi> OdemeTalepleri => Set<OdemeTalebi>();
    public DbSet<OdemeMakbuz> OdemeMakbuzlar => Set<OdemeMakbuz>();
    public DbSet<BankaHesap> BankaHesaplar => Set<BankaHesap>();
    public DbSet<BankaFis> BankaFisler => Set<BankaFis>();
    public DbSet<Cek> Cekler => Set<Cek>();

    // Settings & Meta
    public DbSet<GridLayout> GridLayouts => Set<GridLayout>();
    public DbSet<VersionInfo> VersionInfos => Set<VersionInfo>();
    public DbSet<VersionChange> VersionChanges => Set<VersionChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraints
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<CariHesap>().HasIndex(c => c.CariKod).IsUnique();
        modelBuilder.Entity<Stok>().HasIndex(s => s.StokKod).IsUnique();
        modelBuilder.Entity<GridLayout>()
            .HasIndex(g => new { g.UserId, g.FormName, g.GridName }).IsUnique();

        // Cascade rules
        modelBuilder.Entity<Fatura>()
            .HasMany(f => f.Detaylar)
            .WithOne(d => d.Fatura)
            .HasForeignKey(d => d.FaturaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CariHesap>()
            .HasMany(c => c.Faturalar)
            .WithOne(f => f.CariHesap)
            .HasForeignKey(f => f.CariHesapId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CariHesap>()
            .HasMany(c => c.Ekstreler)
            .WithOne(e => e.CariHesap)
            .HasForeignKey(e => e.CariHesapId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BankaHesap>()
            .HasMany(b => b.Fisler)
            .WithOne(f => f.BankaHesap)
            .HasForeignKey(f => f.BankaHesapId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VersionInfo>()
            .HasMany(v => v.Changes)
            .WithOne(c => c.VersionInfo)
            .HasForeignKey(c => c.VersionInfoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = 1, Name = "Yönetici", Description = "Tam yetki",
                IsAdmin = true, CanEditCariHesaplar = true, CanEditFaturalar = true,
                CanEditStok = true, CanEditOdeme = true, CanEditBanka = true,
                CanManageUsers = true
            },
            new Role
            {
                Id = 2, Name = "Muhasebe", Description = "Muhasebe işlemleri",
                CanEditCariHesaplar = true, CanEditFaturalar = true,
                CanEditOdeme = true, CanEditBanka = true
            },
            new Role
            {
                Id = 3, Name = "Satış", Description = "Satış işlemleri",
                CanEditFaturalar = true, CanEditStok = false
            },
            new Role
            {
                Id = 4, Name = "Depo", Description = "Depo/stok işlemleri",
                CanEditStok = true
            },
            new Role
            {
                Id = 5, Name = "Görüntüleyici", Description = "Sadece görüntüleme"
            }
        );

        // Seed initial version
        modelBuilder.Entity<VersionInfo>().HasData(
            new VersionInfo
            {
                Id = 1,
                Version = "1.0.0",
                Title = "İlk Sürüm",
                ReleaseNotes = "Delphi ERP7V uygulamasından C# platformuna geçiş. Modernize edilmiş arayüz, hatırla beni, RDP oturum takibi ve sürüm kontrolü eklendi.",
                ReleaseDate = new DateTime(2026, 3, 12),
                ReleasedBy = "Asiasoft",
                IsCritical = false
            }
        );

        modelBuilder.Entity<VersionChange>().HasData(
            new VersionChange { Id = 1, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "Delphi ERP7V'den C# WinForms'a tam geçiş", Module = "Genel" },
            new VersionChange { Id = 2, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "Giriş ekranında 'Beni Hatırla' özelliği", Module = "Giriş" },
            new VersionChange { Id = 3, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "RDP oturum takibi - aynı anda kimlerin bağlı olduğunu görme", Module = "Oturum" },
            new VersionChange { Id = 4, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "Sürüm geçmişi ve değişiklik kaydı", Module = "Sürüm" },
            new VersionChange { Id = 5, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "Grid kolon düzenlerini kullanıcı bazlı kaydetme (INI dosyaları yerine veritabanı)", Module = "Grid" },
            new VersionChange { Id = 6, VersionInfoId = 1, ChangeType = ChangeType.NewFeature, Description = "Gelişmiş denetim kaydı (audit log)", Module = "Güvenlik" },
            new VersionChange { Id = 7, VersionInfoId = 1, ChangeType = ChangeType.Improvement, Description = "Cari Hesaplar modülü", Module = "Cari" },
            new VersionChange { Id = 8, VersionInfoId = 1, ChangeType = ChangeType.Improvement, Description = "Fatura modülü (Alış/Satış/İhracat)", Module = "Fatura" },
            new VersionChange { Id = 9, VersionInfoId = 1, ChangeType = ChangeType.Improvement, Description = "Stok modülü", Module = "Stok" },
            new VersionChange { Id = 10, VersionInfoId = 1, ChangeType = ChangeType.Improvement, Description = "Ödeme ve tahsilat modülü", Module = "Ödeme" },
            new VersionChange { Id = 11, VersionInfoId = 1, ChangeType = ChangeType.Improvement, Description = "Banka işlemleri modülü", Module = "Banka" }
        );
    }
}
