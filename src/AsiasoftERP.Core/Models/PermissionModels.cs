using System;
using System.Collections.Generic;

namespace AsiasoftERP.Core.Models;

/// <summary>
/// Definition of a system module or page available for user authorization
/// </summary>
public class ModuleDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string FullTitle => $"{Icon}  {Title}";
}

/// <summary>
/// Central registry of all modules in the Asiasoft ERP system
/// </summary>
public static class AppModuleRegistry
{
    public static readonly List<ModuleDefinition> AllModules = new()
    {
        // 1. GENEL
        new ModuleDefinition { Key = "Home", Title = "Genel Bakış Dashboard", Category = "GENEL", Icon = "🏠", Description = "Şirket genel göstergeleri ve özet paneli" },

        // 2. CARİ & MÜŞTERİ
        new ModuleDefinition { Key = "CariEkstre", Title = "Cari Hesap Ekstresi", Category = "CARİ & MÜŞTERİ", Icon = "⚡", Description = "Cari hareketler ve yürüyen bakiye ekstresi" },
        new ModuleDefinition { Key = "CariKartlar", Title = "Cari Kartları Rehberi", Category = "CARİ & MÜŞTERİ", Icon = "👥", Description = "Müşteri, tedarikçi, müstahsil ve amele cari kartları" },
        new ModuleDefinition { Key = "CariBakiye", Title = "Borç / Alacak Listesi", Category = "CARİ & MÜŞTERİ", Icon = "⚖️", Description = "Toplu cari bakiye ve yaşlandırma listesi" },

        // 3. KANTAR & ÜRETİM
        new ModuleDefinition { Key = "YuklemeProgrami", Title = "Yükleme Programı", Category = "KANTAR & ÜRETİM", Icon = "🚚", Description = "İhracat ve iç piyasa araç yükleme planlaması" },
        new ModuleDefinition { Key = "SeferMaliyet", Title = "Nakliye & Tır Sefer Maliyeti", Category = "KANTAR & ÜRETİM", Icon = "🚛", Description = "Tır seferleri, KM yakıt takibi, harcırah ve sefer kârlılık analizi" },
        new ModuleDefinition { Key = "Puantaj", Title = "Amele Puantaj Defteri", Category = "KANTAR & ÜRETİM", Icon = "📋", Description = "Günlük işçi yevmiye, mesai ve masraf defteri" },
        new ModuleDefinition { Key = "CavusServis", Title = "Çavuş & Servis Eşleme", Category = "KANTAR & ÜRETİM", Icon = "🤝", Description = "Hasat amele çavuşu, kantar ve servis hakediş mutabakatı" },
        new ModuleDefinition { Key = "BahceKesim", Title = "Bahçe Kesim & Alım Sözleşmeleri", Category = "KANTAR & ÜRETİM", Icon = "🌳", Description = "Sözleşmeli bahçe alımları, parsel rekoltesi ve kesim maliyeti" },
        new ModuleDefinition { Key = "MalKabul", Title = "Mal Kabul & Canlı Kantar", Category = "KANTAR & ÜRETİM", Icon = "⚖️", Description = "Canlı indikatör bağlantılı kantar tartımı" },
        new ModuleDefinition { Key = "Kantar", Title = "Kantar / Tartı Fişleri", Category = "KANTAR & ÜRETİM", Icon = "📋", Description = "Geçmiş kantar tartım kayıtları ve fiş listesi" },
        new ModuleDefinition { Key = "Etilen", Title = "Etilen & Sarartma Odaları", Category = "KANTAR & ÜRETİM", Icon = "🧪", Description = "Gazlama odaları, etilen seviyesi ve dozaj takibi" },
        new ModuleDefinition { Key = "Paketleme", Title = "Paketleme & Tartım", Category = "KANTAR & ÜRETİM", Icon = "🏭", Description = "Paketleme hattı ve palet/kutu tartım kayıtları" },

        // 4. STOK & DEPO
        new ModuleDefinition { Key = "StokList", Title = "Stok Kartları", Category = "STOK & DEPO", Icon = "📦", Description = "Meyve, narenciye ve ambalaj stok kartları" },
        new ModuleDefinition { Key = "StokHareket", Title = "Stok Hareketleri", Category = "STOK & DEPO", Icon = "🔄", Description = "Giriş, çıkış ve üretim sarfiyat hareketleri" },
        new ModuleDefinition { Key = "StokEkstre", Title = "Stok Kartı Ekstresi", Category = "STOK & DEPO", Icon = "📊", Description = "Depo bazlı stok bakiye ve hareket ekstresi" },

        // 5. TİCARİ & FATURA
        new ModuleDefinition { Key = "SatisIrsaliye", Title = "Satış İrsaliyeleri", Category = "TİCARİ & FATURA", Icon = "🚚", Description = "Sevkiyat ve satış irsaliyesi düzenleme" },
        new ModuleDefinition { Key = "AlisIrsaliye", Title = "Alış İrsaliyeleri", Category = "TİCARİ & FATURA", Icon = "📥", Description = "Mal kabul ve satın alma irsaliyeleri" },
        new ModuleDefinition { Key = "SatisFatura", Title = "Satış Faturaları", Category = "TİCARİ & FATURA", Icon = "📄", Description = "Yurt içi toptan satış faturaları" },
        new ModuleDefinition { Key = "AlisFatura", Title = "Alış Faturaları", Category = "TİCARİ & FATURA", Icon = "📥", Description = "Tedarikçi alış faturaları ve masraf girişleri" },
        new ModuleDefinition { Key = "IhracatFatura", Title = "İhracat Faturaları", Category = "TİCARİ & FATURA", Icon = "🌐", Description = "Dövizli yurt dışı ihracat faturaları" },
        new ModuleDefinition { Key = "Mustahsil", Title = "Müstahsil Makbuzları", Category = "TİCARİ & FATURA", Icon = "🌾", Description = "Çiftçiden yaş meyve/sebze alım makbuzu ve stopaj kesintileri" },

        // 6. FİNANS & BANKA
        new ModuleDefinition { Key = "KasaBanka", Title = "Kasa & Banka Yönetimi", Category = "FİNANS & BANKA", Icon = "💰", Description = "Genel finans hareketleri ve anlık nakit paneli" },
        new ModuleDefinition { Key = "KasaEkstre", Title = "Kasa Hesap Ekstresi", Category = "FİNANS & BANKA", Icon = "💵", Description = "Kasa bazlı yürüyen bakiyeli hareket ekstresi" },
        new ModuleDefinition { Key = "BankaHesap", Title = "Banka Hesapları", Category = "FİNANS & BANKA", Icon = "🏦", Description = "Banka mevduat hesapları ve havale/EFT ekstresi" },
        new ModuleDefinition { Key = "OdemeTalep", Title = "Ödeme Talep İşlemleri", Category = "FİNANS & BANKA", Icon = "💳", Description = "Onay akışlı cari ödeme talepleri" },
        new ModuleDefinition { Key = "CekList", Title = "Çek & Senet Listesi", Category = "FİNANS & BANKA", Icon = "📝", Description = "Müşteri ve firma çek/senet portföyü" },

        // 7. SİSTEM & YÖNETİM
        new ModuleDefinition { Key = "UserMgmt", Title = "Kullanıcı Yönetimi", Category = "SİSTEM", Icon = "👑", Description = "Kullanıcı hesapları ve şirket erişimleri" },
        new ModuleDefinition { Key = "ModulYetki", Title = "Modül Yetki Yöneticisi", Category = "SİSTEM", Icon = "🔐", Description = "Kişi bazında sayfa ve modül görüntüleme izinleri" },
        new ModuleDefinition { Key = "LoginLogs", Title = "Giriş & Güvenlik Logları", Category = "SİSTEM", Icon = "🛡️", Description = "Sistem giriş denetimleri ve IP kayıtları" }
    };
}

/// <summary>
/// DTO representing an individual user's permission for a module
/// </summary>
public class UserModulePermissionDto
{
    public string UserCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool HasPermission { get; set; } = true;
}
