using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

public static class KasaBankaReportRenderer
{
    public static PrintDocument CreatePrintDocument(FinansFisGirisDto fis)
    {
        var doc = new PrintDocument();
        doc.DocumentName = $"{fis.IslemTipi}_Makbuzu_{fis.FisNo}";
        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // Standard A4
        doc.DefaultPageSettings.Margins = new Margins(25, 25, 25, 25);

        doc.PrintPage += (s, e) =>
        {
            var bounds = e.MarginBounds;
            Render(e.Graphics!, fis, bounds);
            e.HasMorePages = false;
        };

        return doc;
    }

    public static Bitmap RenderToBitmap(FinansFisGirisDto fis, int width = 1240, int height = 1754)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        Render(g, fis, new Rectangle(35, 35, width - 70, height - 70));
        return bmp;
    }

    public static void Render(Graphics g, FinansFisGirisDto fis, Rectangle bounds)
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        string docTitle;
        Color accentColor;
        Color accentBg;

        switch (fis.IslemTipi)
        {
            case FinansIslemTipi.KasaTahsilat:
                docTitle = "KASA TAHSİLAT MAKBUZU";
                accentColor = Color.FromArgb(16, 120, 70);
                accentBg = Color.FromArgb(235, 248, 240);
                break;
            case FinansIslemTipi.KasaTediye:
                docTitle = "KASA TEDİYE (ÖDEME) MAKBUZU";
                accentColor = Color.FromArgb(185, 28, 28);
                accentBg = Color.FromArgb(254, 242, 242);
                break;
            case FinansIslemTipi.BankaTahsilat:
                docTitle = "BANKA TAHSİLAT (GELEN HAVALE) DEKONTU";
                accentColor = Color.FromArgb(2, 132, 199);
                accentBg = Color.FromArgb(240, 249, 255);
                break;
            default:
                docTitle = "BANKA TEDİYE (GÖNDERİLEN HAVALE) DEKONTU";
                accentColor = Color.FromArgb(79, 70, 229);
                accentBg = Color.FromArgb(238, 242, 255);
                break;
        }

        using var fontTitle = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontSection = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var fontDocTitle = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        using var fontDataBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontData = new Font("Segoe UI", 9f, FontStyle.Regular);
        using var fontBigAmount = new Font("Segoe UI", 18f, FontStyle.Bold);

        using var penBorder = new Pen(Color.FromArgb(180, 190, 205), 1.2f);
        using var penThin = new Pen(Color.FromArgb(215, 225, 235), 0.8f);
        using var brushDark = new SolidBrush(Color.FromArgb(20, 30, 45));
        using var brushSlate = new SolidBrush(Color.FromArgb(70, 80, 95));
        using var brushHdrBg = new SolidBrush(Color.FromArgb(244, 247, 251));
        using var brushAccent = new SolidBrush(accentColor);
        using var brushAccentBg = new SolidBrush(accentBg);

        int x = bounds.X;
        int y = bounds.Y;
        int w = bounds.Width;

        // 1. TOP HEADER BANNER
        int headerH = 100;
        g.FillRectangle(brushHdrBg, x, y, w, headerH);
        g.DrawRectangle(penBorder, x, y, w, headerH);

        int textLeft = x + 15;
        try
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "tekasya.jpg");
            if (!File.Exists(imgPath))
                imgPath = @"D:\AsiaSoft\src\AsiasoftERP.UI\Resources\Images\tekasya.jpg";
            if (File.Exists(imgPath))
            {
                using var logoImg = Image.FromFile(imgPath);
                g.DrawImage(logoImg, x + 12, y + 12, 76, 76);
                textLeft = x + 100;
            }
        }
        catch { }

        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontTitle, brushDark, textLeft, y + 10);
        g.DrawString("Adres: Karaali Mah. Park Apt. No: 4 Antakya / HATAY\nTel: +90 (326) 221 38 48  •  Faks: +90 (326) 221 38 58\nVergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53", fontSub, brushSlate, textLeft, y + 40);

        // Document Right Box
        int docBoxW = 280;
        int docBoxX = x + w - docBoxW;
        g.FillRectangle(brushAccentBg, docBoxX, y, docBoxW, headerH);
        g.DrawRectangle(penBorder, docBoxX, y, docBoxW, headerH);

        using var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString(docTitle, fontDocTitle, brushAccent, docBoxX + docBoxW / 2, y + 10, sfCenter);

        int docDetailsY = y + 42;
        g.DrawString($"Fiş / Makbuz No : #{fis.FisNo:N0}  /  {fis.MakbuzNo:N0}", fontDataBold, brushDark, docBoxX + 12, docDetailsY);
        g.DrawString($"İşlem Tarihi    : {fis.FisTar:dd.MM.yyyy}  {fis.Saat:hh\\:mm}", fontData, brushDark, docBoxX + 12, docDetailsY + 20);
        string hesapAdi = fis.IslemTipi.ToString().StartsWith("Kasa") ? fis.KasaAdi : fis.BankaAdi;
        g.DrawString($"İlgili Hesap    : {hesapAdi}", fontDataBold, brushDark, docBoxX + 12, docDetailsY + 38);

        y += headerH + 12;

        // 2. PARTIES & TRANSACTION DETAILS (2 BOXES)
        int infoH = 130;
        int boxW = (w - 12) / 2;

        // Box 1: Cari / Şahıs Bilgileri
        g.FillRectangle(brushHdrBg, x, y, boxW, 26);
        g.DrawRectangle(penBorder, x, y, boxW, infoH);
        g.DrawString("İŞLEM YAPILAN CARİ HESAP / ŞAHIS BİLGİLERİ", fontSection, brushDark, x + 10, y + 5);

        int cy = y + 34;
        g.DrawString($"Cari / Firma : {fis.CariAdi}", fontDataBold, brushDark, new RectangleF(x + 10, cy, boxW - 20, 36));
        cy += 38;
        string adr = !string.IsNullOrWhiteSpace(fis.CariAdresi) ? fis.CariAdresi : "HATAY / TÜRKİYE";
        g.DrawString($"Adres        : {adr}", fontData, brushSlate, new RectangleF(x + 10, cy, boxW - 20, 26));
        cy += 28;
        g.DrawString($"V.D. / VKN   : {fis.VergiDairesi} - {fis.VergiNo}", fontData, brushDark, x + 10, cy);

        // Box 2: Finans & Hesap Detayları
        int box2X = x + boxW + 12;
        g.FillRectangle(brushHdrBg, box2X, y, boxW, 26);
        g.DrawRectangle(penBorder, box2X, y, boxW, infoH);
        g.DrawString("FİNANS & HESAP BİLGİLERİ", fontSection, brushDark, box2X + 10, y + 5);

        int fy = y + 34;
        g.DrawString($"İşlem Türü   : {docTitle}", fontDataBold, brushAccent, box2X + 10, fy);
        fy += 22;
        g.DrawString($"Kasa / Banka : {hesapAdi}", fontDataBold, brushDark, box2X + 10, fy);
        fy += 22;
        string bNo = !string.IsNullOrWhiteSpace(fis.BelgeNo) ? fis.BelgeNo : "(Serbest Giriş)";
        g.DrawString($"Dekont / No  : {bNo}", fontData, brushSlate, box2X + 10, fy);
        fy += 22;
        g.DrawString($"Düzenleyen   : {fis.User}", fontData, brushDark, box2X + 10, fy);

        y += infoH + 12;

        // 3. BIG AMOUNT BOX (PARASAL DEĞER VE YAZIYLA TUTAR)
        int amtBoxH = 110;
        g.FillRectangle(brushAccentBg, x, y, w, amtBoxH);
        g.DrawRectangle(penBorder, x, y, w, amtBoxH);

        g.DrawString("İŞLEM TUTARI", fontSection, brushAccent, x + 16, y + 10);
        g.DrawString($"{fis.Tutar:N2} {fis.DovizTuru}", fontBigAmount, brushAccent, x + 16, y + 32);

        // Yazıyla Tutar
        string yazi = !string.IsNullOrWhiteSpace(fis.YaziIle) ? fis.YaziIle : $"{fis.Tutar:N2} {fis.DovizTuru}";
        g.DrawString($"Yalnız:  #{yazi}#", fontDataBold, brushDark, x + 16, y + 78);

        y += amtBoxH + 12;

        // 4. DESCRIPTION & NOTES BOX
        int ackH = 90;
        g.FillRectangle(brushHdrBg, x, y, w, 26);
        g.DrawRectangle(penBorder, x, y, w, ackH);
        g.DrawString("AÇIKLAMA / MASRAF / SEFER VE ÖDEME NOTLARI", fontSection, brushDark, x + 10, y + 5);

        string aciklamaText = !string.IsNullOrWhiteSpace(fis.Aciklama) 
            ? fis.Aciklama 
            : $"{fis.CariAdi} firmasına ait {docTitle} işlemi.";
        g.DrawString(aciklamaText, fontData, brushDark, new RectangleF(x + 12, y + 34, w - 24, ackH - 40));

        y += ackH + 20;

        // 5. SIGNATURES (3 BOXES)
        int signH = 130;
        int signW = (w - 20) / 3;

        // Box 1: Tahsil Eden / Düzenleyen
        g.DrawRectangle(penBorder, x, y, signW, signH);
        g.FillRectangle(brushHdrBg, x, y, signW, 26);
        g.DrawString("İŞLEMİ YAPAN (Kasiyer / Yetkili)", fontSection, brushDark, x + 8, y + 5);
        g.DrawString($"Ad Soyad : {fis.User}\nGörev    : Vezne / Muhasebe\nİmza:\n\nKaşe:", fontSub, brushSlate, x + 10, y + 36);

        // Box 2: Teslim Eden / Alan
        int s2X = x + signW + 10;
        g.DrawRectangle(penBorder, s2X, y, signW, signH);
        g.FillRectangle(brushHdrBg, s2X, y, signW, 26);
        g.DrawString("MÜŞTERİ / MUHATAP / TAŞIYICI", fontSection, brushDark, s2X + 8, y + 5);
        g.DrawString($"Unvan / Şahıs : {fis.CariAdi}\nT.C. / VKN     : {fis.VergiNo}\nİmza:\n\nKaşe:", fontSub, brushSlate, s2X + 10, y + 36);

        // Box 3: Mali İşler Onayı
        int s3X = s2X + signW + 10;
        g.DrawRectangle(penBorder, s3X, y, signW, signH);
        g.FillRectangle(brushHdrBg, s3X, y, signW, 26);
        g.DrawString("MALİ İŞLER & YÖNETİM ONAYI", fontSection, brushDark, s3X + 8, y + 5);
        g.DrawString("Onay Durumu : ONAYLANDI\nKontrol Eden: Muhasebe Müdürü\nİmza:\n\nKaşe:", fontSub, brushSlate, s3X + 10, y + 36);

        // Footer Note
        y += signH + 16;
        g.DrawString("İşbu makbuz 213 sayılı Vergi Usul Kanunu ve Türk Ticaret Kanunu hükümleri doğrultusunda tahsilat veya tediye kaydı amacıyla düzenlenmiştir.", fontSub, brushSlate, x, y);
    }
}
