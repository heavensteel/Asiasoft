using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AsiasoftERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankaHesaplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HesapKod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HesapAdi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BankaAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SubeAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Iban = table.Column<string>(type: "TEXT", maxLength: 26, nullable: true),
                    HesapNumarasi = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaHesaplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CariHesaplar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CariKod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CariAdi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CariTip = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Adres = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Sehir = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Ulke = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Fax = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VergiNo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    VergiDairesi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    KrediLimiti = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizBakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TemsilciAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariHesaplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CanViewCariHesaplar = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditCariHesaplar = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewFaturalar = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditFaturalar = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewStok = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditStok = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewOdeme = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditOdeme = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewBanka = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanEditBanka = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewRapor = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanManageUsers = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanViewVersionHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stoklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StokKod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StokAdi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StokGrubu = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StokAlGrubu = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Birim = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Miktar = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinMiktar = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MaxMiktar = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AlisFiyat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SatisFiyat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    KdvYuzde = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stoklar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReleasedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankaFisler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BankaHesapId = table.Column<int>(type: "INTEGER", nullable: false),
                    FisNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FisTip = table.Column<int>(type: "INTEGER", nullable: false),
                    FisTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    DovizKuru = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: true),
                    GonderenAlici = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsOnaylandi = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankaFisler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankaFisler_BankaHesaplar_BankaHesapId",
                        column: x => x.BankaHesapId,
                        principalTable: "BankaHesaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankaFisler_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CariEkstreler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FisNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IslemTipi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Borc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Alacak = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    DovizBorc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizAlacak = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizBakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferansId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReferansTip = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariEkstreler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariEkstreler_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cekler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CekNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CekTipi = table.Column<int>(type: "INTEGER", nullable: false),
                    DuzenlemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: true),
                    Keside = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Muhatap = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cekler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cekler_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Faturalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FisNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FaturaTipi = table.Column<int>(type: "INTEGER", nullable: false),
                    FaturaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    DovizKuru = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FisTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IskontaTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetFisTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtvToplami = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TevkifatTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GenelToplam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OdemeSekli = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsOdendi = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsKapali = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturalar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faturalar_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OdemeMakbuzlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MakbuzNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MakbuzTipi = table.Column<int>(type: "INTEGER", nullable: false),
                    MakbuzTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OdemeYontemi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BankaAdi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HesapNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsKapali = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeMakbuzlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeMakbuzlar_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdemeTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TalepNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TalepTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VadeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OdenenTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Bakiye = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Durum = table.Column<int>(type: "INTEGER", nullable: false),
                    IsyeriKod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    OdemeYontemi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdemeTalepleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdemeTalepleri_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RememberMe = table.Column<bool>(type: "INTEGER", nullable: false),
                    RememberMeToken = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    TokenExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginMachine = table.Column<string>(type: "TEXT", nullable: true),
                    LastLoginSession = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokHareketler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StokId = table.Column<int>(type: "INTEGER", nullable: false),
                    FisNo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HareketTip = table.Column<int>(type: "INTEGER", nullable: false),
                    HareketTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Miktar = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DovizTipi = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    DovizKuru = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CariHesapId = table.Column<int>(type: "INTEGER", nullable: true),
                    FaturaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokHareketler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokHareketler_CariHesaplar_CariHesapId",
                        column: x => x.CariHesapId,
                        principalTable: "CariHesaplar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StokHareketler_Stoklar_StokId",
                        column: x => x.StokId,
                        principalTable: "Stoklar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VersionChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionInfoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Module = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionChanges_VersionInfos_VersionInfoId",
                        column: x => x.VersionInfoId,
                        principalTable: "VersionInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaturaDetaylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FaturaId = table.Column<int>(type: "INTEGER", nullable: false),
                    StokId = table.Column<int>(type: "INTEGER", nullable: true),
                    UrunAdi = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UrunKod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Birim = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Miktar = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IskontoYuzde = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IskontoTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KdvYuzde = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KdvTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ToplamTutar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SiraNo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaturaDetaylar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaturaDetaylar_Faturalar_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaturaDetaylar_Stoklar_StokId",
                        column: x => x.StokId,
                        principalTable: "Stoklar",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    MachineName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GridLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GridName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LayoutJson = table.Column<string>(type: "TEXT", nullable: false),
                    FilterActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilterJson = table.Column<string>(type: "TEXT", nullable: true),
                    SortJson = table.Column<string>(type: "TEXT", nullable: true),
                    GroupByJson = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GridLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GridLayouts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    MachineName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SessionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RdpSessionId = table.Column<int>(type: "INTEGER", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LoginTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogoutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentModule = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CanEditBanka", "CanEditCariHesaplar", "CanEditFaturalar", "CanEditOdeme", "CanEditStok", "CanManageUsers", "CanViewBanka", "CanViewCariHesaplar", "CanViewFaturalar", "CanViewOdeme", "CanViewRapor", "CanViewStok", "CanViewVersionHistory", "Description", "IsAdmin", "Name" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, true, true, true, true, true, true, true, true, "Tam yetki", true, "Yönetici" },
                    { 2, true, true, true, true, false, false, true, true, true, true, true, true, true, "Muhasebe işlemleri", false, "Muhasebe" },
                    { 3, false, false, true, false, false, false, true, true, true, true, true, true, true, "Satış işlemleri", false, "Satış" },
                    { 4, false, false, false, false, true, false, true, true, true, true, true, true, true, "Depo/stok işlemleri", false, "Depo" },
                    { 5, false, false, false, false, false, false, true, true, true, true, true, true, true, "Sadece görüntüleme", false, "Görüntüleyici" }
                });

            migrationBuilder.InsertData(
                table: "VersionInfos",
                columns: new[] { "Id", "IsCritical", "ReleaseDate", "ReleaseNotes", "ReleasedBy", "Title", "Version" },
                values: new object[] { 1, false, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Delphi ERP7V uygulamasından C# platformuna geçiş. Modernize edilmiş arayüz, hatırla beni, RDP oturum takibi ve sürüm kontrolü eklendi.", "Asiasoft", "İlk Sürüm", "1.0.0" });

            migrationBuilder.InsertData(
                table: "VersionChanges",
                columns: new[] { "Id", "ChangeType", "Description", "Module", "VersionInfoId" },
                values: new object[,]
                {
                    { 1, 1, "Delphi ERP7V'den C# WinForms'a tam geçiş", "Genel", 1 },
                    { 2, 1, "Giriş ekranında 'Beni Hatırla' özelliği", "Giriş", 1 },
                    { 3, 1, "RDP oturum takibi - aynı anda kimlerin bağlı olduğunu görme", "Oturum", 1 },
                    { 4, 1, "Sürüm geçmişi ve değişiklik kaydı", "Sürüm", 1 },
                    { 5, 1, "Grid kolon düzenlerini kullanıcı bazlı kaydetme (INI dosyaları yerine veritabanı)", "Grid", 1 },
                    { 6, 1, "Gelişmiş denetim kaydı (audit log)", "Güvenlik", 1 },
                    { 7, 3, "Cari Hesaplar modülü", "Cari", 1 },
                    { 8, 3, "Fatura modülü (Alış/Satış/İhracat)", "Fatura", 1 },
                    { 9, 3, "Stok modülü", "Stok", 1 },
                    { 10, 3, "Ödeme ve tahsilat modülü", "Ödeme", 1 },
                    { 11, 3, "Banka işlemleri modülü", "Banka", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaFisler_BankaHesapId",
                table: "BankaFisler",
                column: "BankaHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_BankaFisler_CariHesapId",
                table: "BankaFisler",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_CariEkstreler_CariHesapId",
                table: "CariEkstreler",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_CariHesaplar_CariKod",
                table: "CariHesaplar",
                column: "CariKod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cekler_CariHesapId",
                table: "Cekler",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaDetaylar_FaturaId",
                table: "FaturaDetaylar",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FaturaDetaylar_StokId",
                table: "FaturaDetaylar",
                column: "StokId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturalar_CariHesapId",
                table: "Faturalar",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_GridLayouts_UserId_FormName_GridName",
                table: "GridLayouts",
                columns: new[] { "UserId", "FormName", "GridName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdemeMakbuzlar_CariHesapId",
                table: "OdemeMakbuzlar",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_OdemeTalepleri_CariHesapId",
                table: "OdemeTalepleri",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_CariHesapId",
                table: "StokHareketler",
                column: "CariHesapId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketler_StokId",
                table: "StokHareketler",
                column: "StokId");

            migrationBuilder.CreateIndex(
                name: "IX_Stoklar_StokKod",
                table: "Stoklar",
                column: "StokKod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionChanges_VersionInfoId",
                table: "VersionChanges",
                column: "VersionInfoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BankaFisler");

            migrationBuilder.DropTable(
                name: "CariEkstreler");

            migrationBuilder.DropTable(
                name: "Cekler");

            migrationBuilder.DropTable(
                name: "FaturaDetaylar");

            migrationBuilder.DropTable(
                name: "GridLayouts");

            migrationBuilder.DropTable(
                name: "OdemeMakbuzlar");

            migrationBuilder.DropTable(
                name: "OdemeTalepleri");

            migrationBuilder.DropTable(
                name: "StokHareketler");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "VersionChanges");

            migrationBuilder.DropTable(
                name: "BankaHesaplar");

            migrationBuilder.DropTable(
                name: "Faturalar");

            migrationBuilder.DropTable(
                name: "Stoklar");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VersionInfos");

            migrationBuilder.DropTable(
                name: "CariHesaplar");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
