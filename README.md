# Asiasoft ERP — C# .NET 9 Remake

Modern C# yeniden yazımı — orijinal Delphi ERP7V uygulamasından.

---

## 🚀 Proje Yapısı

```
AsiasoftRemake/
├── src/
│   ├── AsiasoftERP.Core/         # Domain modelleri
│   │   └── Models/               # ERP entity sınıfları
│   ├── AsiasoftERP.Data/         # Veri erişim katmanı
│   │   ├── AppDbContext.cs       # EF Core DbContext
│   │   ├── Services/             # İş servisleri
│   │   └── Migrations/           # EF Core migrasyonları
│   └── AsiasoftERP.UI/           # WinForms kullanıcı arayüzü
│       ├── Forms/                # Form dosyaları
│       │   └── Modules/          # ERP modül formları
│       └── Helpers/              # Yardımcı sınıflar
└── ERPV7/                        # Orijinal Delphi uygulama dosyaları
```

---

## ✨ Yeni Özellikler (Delphi'ye göre iyileştirmeler)

### 🔐 Giriş & Güvenlik
- **Beni Hatırla (30 gün)** — Token tabanlı otomatik giriş (Windows Registry)
- BCrypt ile güvenli şifre hashleme
- Rol tabanlı yetkilendirme (5 ayrı rol: Yönetici, Muhasebe, Satış, Depo, Görüntüleyici)

### 🖥️ RDP Oturum Takibi
- Windows Terminal Services API (WTSApi32) ile RDP oturumu tespiti
- Aktif Oturumlar ekranında hem ERP kullanıcılarını hem de Windows RDP oturumlarını görme
- Duruma göre otomatik 30 saniyede bir yenileme
- Status bar'da mevcut oturum bilgisi ve [RDP] etiketi

### 📋 Sürüm Kontrolü (Version Control)
- Veritabanında sürüm geçmişi kaydı
- Her sürüm için değişiklik listesi (Yeni Özellik, Hata Düzeltme, İyileştirme, Güvenlik)
- Yöneticiler yeni sürüm ekleyebilir
- Güncel sürüm yeşil ile vurgulanır

### 📊 Grid Düzeni Kalıcılığı
- Orijinal INI dosyaları yerine **veritabanında** kullanıcı başına grid düzeni
- Her kullanıcı kendi kolon genişliklerini/sırasını/görünürlüğünü kaydedebilir
- "Düzen Kaydet" butonu ile anlık kayıt

### 📝 Denetim Kaydı (Audit Log)
- Tüm giriş/çıkış işlemleri kaydedilir
- CRUD operasyonları için JSON formatında eski/yeni değer takibi
- Modüle ve tarihe göre filtrelenebilir Audit Log ekranı

---

## 📦 Modüller

| Modül | Açıklama |
|-------|----------|
| 📋 **Cari Hesaplar** | Müşteri/tedarikçi yönetimi, bakiye, ekstresi |
| 🧾 **Faturalar** | Alış, Satış, İhracat, Konsinyasyon faturaları |
| 📦 **Stok** | Stok listesi, hareketler, giriş/çıkış raporu |
| 💳 **Ödeme** | Ödeme talepleri, planı, makbuzlar, tahsilat, SMM |
| 🏦 **Banka** | Hesaplar, işlem fişleri, virman, döviz, çekler, havaleler |
| 👤 **Kullanıcı Yönetimi** | Kullanıcı ekleme/düzenleme, şifre sıfırlama |
| 🖥️ **Aktif Oturumlar** | ERP + Windows RDP oturum takibi |
| 📝 **Denetim Kaydı** | Tüm sistem işlemlerinin kaydı |
| 🏷️ **Sürüm Geçmişi** | Versiyon takibi ve değişiklik notları |

---

## 🛠️ Teknik Stack

- **Platform**: .NET 9.0 WinForms
- **Veritabanı**: SQLite (varsayılan) veya SQL Server
- **ORM**: Entity Framework Core 9.0
- **Şifreleme**: BCrypt.Net-Next
- **Serileştirme**: Newtonsoft.Json
- **RDP**: Windows WTSApi32 P/Invoke

---

## 🚀 Kurulum

### Gereksinimler
- .NET 9.0 Runtime
- Windows 10/11 veya Windows Server 2016+

### Çalıştırma
```bash
cd src/AsiasoftERP.UI
dotnet run
```

### İlk Giriş
- Kullanıcı adı: `admin`
- Şifre: `Admin123!`

> ⚠️ İlk girişten sonra şifrenizi değiştirmeyi unutmayın!

---

## 🔧 Yapılandırma

Veritabanı dosyası otomatik olarak oluşturulur:
```
%APPDATA%\AsiasoftERP\erp.db
```

SQL Server kullanmak için `Program.cs`'de şu satırı değiştirin:
```csharp
DbContextFactory.Configure(connectionString, useSqlite: false);
```

---

## 📂 Orijinal Delphi Dosyaları

`ERPV7/` klasöründeki `.ini` dosyaları orijinal Delphi uygulamasının grid düzeni ayarlarıdır.
Dosya adı formatı: `{KullanıcıID}_{FormAdı}{Yıl}.ini`

Bu dosyalar artık veritabanında `GridLayouts` tablosunda saklanmaktadır.

---

## 📋 Versiyon Geçmişi

### v1.0.0 (12.03.2026)
- İlk sürüm — Delphi ERP7V'den C# WinForms'a tam geçiş
- Beni Hatırla özelliği eklendi
- RDP oturum takibi eklendi
- Sürüm geçmişi eklendi
- Grid düzeni veritabanına taşındı
- Denetim kaydı (Audit Log) eklendi

---

© 2026 Asiasoft — Tüm hakları saklıdır.
