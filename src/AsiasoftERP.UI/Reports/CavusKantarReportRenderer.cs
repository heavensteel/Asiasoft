using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Reports;

/// <summary>
/// High-resolution vector A4 renderer for Çavuş & Servis Hasat Sefer Hakediş İcmali
/// </summary>
public static class CavusKantarReportRenderer
{
    public static void PrintReport(List<CavusKantarEslesmeDto> list, string cavusAdi, string bolge, DateTime start, DateTime end)
    {
        var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // 210mm x 297mm
        pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
        pd.DefaultPageSettings.Landscape = false;

        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                Render(e.Graphics, e.MarginBounds, list, cavusAdi, bolge, start, end);
            }
            e.HasMorePages = false;
        };

        using var dlg = new PrintPreviewDialog
        {
            Document = pd,
            Width = 1000,
            Height = 800,
            StartPosition = FormStartPosition.CenterScreen
        };
        dlg.ShowDialog();
    }

    public static Bitmap RenderToBitmap(List<CavusKantarEslesmeDto> list, string cavusAdi, string bolge, DateTime start, DateTime end, int width = 1240, int height = 1754)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(50, 50, width - 100, height - 100);
        Render(g, bounds, list, cavusAdi, bolge, start, end);

        return bmp;
    }

    public static void Render(Graphics g, Rectangle bounds, List<CavusKantarEslesmeDto> list, string cavusAdi, string bolge, DateTime start, DateTime end)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        using var fontHeader = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.5f);
        using var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 8.5f);
        using var fontKpiVal = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var fontKpiLbl = new Font("Segoe UI", 7.5f, FontStyle.Bold);

        using var penBorder = new Pen(Color.FromArgb(203, 213, 225), 1f);
        using var penDark = new Pen(Color.FromArgb(51, 65, 85), 1.5f);
        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var brushTeal = new SolidBrush(Color.FromArgb(13, 148, 136));
        using var brushGreen = new SolidBrush(Color.FromArgb(16, 120, 70));

        int x = bounds.Left;
        int y = bounds.Top;
        int w = bounds.Width;

        // 1. TOP HEADER & LOGO
        int headerHeight = 70;
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "tekasya_logo.png");
        if (File.Exists(logoPath))
        {
            try
            {
                using var logo = Image.FromFile(logoPath);
                g.DrawImage(logo, new Rectangle(x, y + 2, 60, 60));
            }
            catch { }
        }
        else
        {
            using var brushCircle = new SolidBrush(Color.FromArgb(16, 120, 70));
            g.FillEllipse(brushCircle, x, y + 2, 58, 58);
            using var fontCircle = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var brushW = new SolidBrush(Color.White);
            g.DrawString("TA", fontCircle, brushW, x + 16, y + 17);
        }

        // Title and company info
        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontHeader, brushDark, x + 72, y + 2);
        g.DrawString("Adres: Karaali Mah. Park Apt. No:4 Antakya / HATAY  •  Tel: +90 (326) 221 38 48", fontSub, brushMuted, x + 72, y + 24);
        g.DrawString("Vergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53", fontSub, brushMuted, x + 72, y + 39);

        // Document Badge
        int badgeW = 280;
        int badgeH = 58;
        var badgeRect = new Rectangle(x + w - badgeW, y + 2, badgeW, badgeH);
        using var brushBadgeBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penBadge = new Pen(Color.FromArgb(134, 239, 172), 1f);
        g.FillRectangle(brushBadgeBg, badgeRect);
        g.DrawRectangle(penBadge, badgeRect);

        using var fontDocTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var fontDocSub = new Font("Segoe UI", 8f);
        g.DrawString("ÇAVUŞ & SERVİS HAKEDİŞ İCMALİ", fontDocTitle, brushGreen, badgeRect.X + 12, badgeRect.Y + 6);
        g.DrawString($"Dönem: {start:dd.MM.yyyy} — {end:dd.MM.yyyy}", fontDocSub, brushDark, badgeRect.X + 12, badgeRect.Y + 26);
        g.DrawString($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}", fontDocSub, brushMuted, badgeRect.X + 12, badgeRect.Y + 41);

        y += headerHeight + 12;

        // 2. INFORMATION CARDS
        int cardH = 68;
        int halfW = (w - 12) / 2;

        // Left Card: Çavuş & Güzergah
        var cardLeft = new Rectangle(x, y, halfW, cardH);
        using var brushCardBg = new SolidBrush(Color.FromArgb(248, 250, 252));
        g.FillRectangle(brushCardBg, cardLeft);
        g.DrawRectangle(penBorder, cardLeft);

        g.DrawString("HASAT EKİP & GÜZERGAH BİLGİSİ", fontKpiLbl, brushMuted, cardLeft.X + 10, cardLeft.Y + 8);
        g.DrawString($"Hasat Çavuşu: {(!string.IsNullOrEmpty(cavusAdi) ? cavusAdi : "TÜM EKİPLER (GENEL İCMAL)")}", fontBold, brushDark, cardLeft.X + 10, cardLeft.Y + 24);
        g.DrawString($"Güzergah / Bölge: {(!string.IsNullOrEmpty(bolge) ? bolge : "Tüm Bölgeler")}", fontSub, brushDark, cardLeft.X + 10, cardLeft.Y + 44);

        // Right Card: 4 KPI Badges
        var cardRight = new Rectangle(x + halfW + 12, y, halfW, cardH);
        g.FillRectangle(brushCardBg, cardRight);
        g.DrawRectangle(penBorder, cardRight);

        double totalNetKg = list.Sum(s => s.NetKg);
        double totalServisAdet = list.Sum(s => s.ServisSayisi);
        double totalServisTutar = list.Sum(s => s.ServisTutari);

        int kpiW = halfW / 3;
        // KPI 1: Sefer
        g.DrawString("TOPLAM SEFER", fontKpiLbl, brushMuted, cardRight.X + 10, cardRight.Y + 8);
        g.DrawString($"{list.Count} Sefer", fontKpiVal, brushDark, cardRight.X + 10, cardRight.Y + 26);

        // KPI 2: Hasat KG
        g.DrawString("TOPLAM HASAT", fontKpiLbl, brushMuted, cardRight.X + kpiW + 10, cardRight.Y + 8);
        g.DrawString($"{(totalNetKg / 1000.0):N1} Ton", fontKpiVal, brushTeal, cardRight.X + kpiW + 10, cardRight.Y + 26);

        // KPI 3: Servis Tutarı
        g.DrawString("SERVİS HAKEDİŞİ", fontKpiLbl, brushMuted, cardRight.X + (kpiW * 2) + 10, cardRight.Y + 8);
        g.DrawString($"{totalServisTutar:N0} TL", fontKpiVal, brushGreen, cardRight.X + (kpiW * 2) + 10, cardRight.Y + 26);

        y += cardH + 16;

        // 3. TABLE HEADER
        int thH = 24;
        var thRect = new Rectangle(x, y, w, thH);
        using var brushTh = new SolidBrush(Color.FromArgb(241, 245, 249));
        g.FillRectangle(brushTh, thRect);
        g.DrawRectangle(penBorder, thRect);

        // Sütun genişlikleri (Toplam 1140 px = A4 genişliği)
        int[] colWidths = { 40, 65, 75, 100, 140, 170, 240, 140, 90, 60, 120 };
        string[] colTitles = { "#", "Fiş No", "Tarih", "Plaka", "Şoför", "Ürün / Cins", "Bahçe / Müstahsil", "Çavuş", "Net KG", "Servis", "Tutar (TL)" };
        StringAlignment[] colAlign = { 
            StringAlignment.Center, StringAlignment.Center, StringAlignment.Center, StringAlignment.Near, 
            StringAlignment.Near, StringAlignment.Near, StringAlignment.Near, StringAlignment.Near, 
            StringAlignment.Far, StringAlignment.Center, StringAlignment.Far 
        };

        int curX = x;
        for (int i = 0; i < colWidths.Length; i++)
        {
            var r = new Rectangle(curX, y + 3, colWidths[i], thH - 6);
            using var sf = new StringFormat { Alignment = colAlign[i], LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(colTitles[i], fontKpiLbl, brushDark, r, sf);
            curX += colWidths[i];
        }

        y += thH;

        // 4. TABLE ROWS
        int rowH = 24;
        int rowCount = Math.Min(list.Count, 32); // fit cleanly on A4 single page
        for (int i = 0; i < rowCount; i++)
        {
            var item = list[i];
            var rowRect = new Rectangle(x, y, w, rowH);

            if (i % 2 == 1)
            {
                using var brushRowAlt = new SolidBrush(Color.FromArgb(248, 250, 252));
                g.FillRectangle(brushRowAlt, rowRect);
            }
            g.DrawLine(penBorder, x, y + rowH, x + w, y + rowH);

            curX = x;
            string[] vals = {
                (i + 1).ToString(),
                item.GirisKodu > 0 ? item.GirisKodu.ToString() : item.IdNo.ToString(),
                item.Tarih.ToString("dd.MM.yy"),
                item.Plaka,
                item.SoforAdi,
                item.UrunAdi,
                item.MustahsilAdi,
                !string.IsNullOrEmpty(item.CavusAdi) ? item.CavusAdi : "-",
                item.NetKg.ToString("N0"),
                item.ServisSayisi > 0 ? item.ServisSayisi.ToString("0.#") : "-",
                item.ServisTutari > 0 ? item.ServisTutari.ToString("N0") : "-"
            };

            for (int col = 0; col < colWidths.Length; col++)
            {
                var r = new Rectangle(curX + 3, y, colWidths[col] - 6, rowH);
                using var sf = new StringFormat { Alignment = colAlign[col], LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                
                Brush textBrush = col == 10 && item.ServisTutari > 0 ? brushGreen :
                                  col == 8 ? brushTeal : brushDark;

                Font rowFont = col == 8 || col == 10 ? fontBold : fontSub;
                g.DrawString(vals[col], rowFont, textBrush, r, sf);
                curX += colWidths[col];
            }

            y += rowH;
        }

        // 5. GRAND TOTALS ROW
        y += 4;
        var totalRect = new Rectangle(x, y, w, 26);
        using var brushTotalBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penTotal = new Pen(Color.FromArgb(134, 239, 172), 1.5f);
        g.FillRectangle(brushTotalBg, totalRect);
        g.DrawRectangle(penTotal, totalRect);

        g.DrawString($"GENEL TOPLAM ({list.Count} Sefer):", fontBold, brushDark, x + 12, y + 4);
        g.DrawString($"Toplam Hasat: {totalNetKg:N0} KG  ({(totalNetKg / 1000.0):N2} Ton)", fontBold, brushTeal, x + 300, y + 4);
        g.DrawString($"Toplam Servis: {totalServisAdet:N0} Sefer", fontBold, brushDark, x + 620, y + 4);
        g.DrawString($"ÖDENECEK HAKEDİŞ: {totalServisTutar:N2} TL", fontBold, brushGreen, x + w - 240, y + 4);

        y += 42;

        // 6. LEGAL NOTICE
        g.DrawString("İşbu icmal; bahçe hasat kantar tartımları, amele çavuşu seferleri ve taşıyıcı servis hakedişlerinin çapraz mutabakatı sonucunda düzenlenmiştir.", fontSub, brushMuted, x + 5, y);
        y += 20;

        // 7. SIGNATURE BOXES (3 Columns)
        int sigH = 80;
        int sigW = (w - 24) / 3;

        string[] sigTitles = {
            "HASAT ÇAVUŞU (HAKEDİŞ SAHİBİ)",
            "SAHA & KANTAR SORUMLUSU",
            "MALİ İŞLER & MUHASEBE ONAYI"
        };
        string[] sigSub = {
            $"Ad Soyad: {(!string.IsNullOrEmpty(cavusAdi) ? cavusAdi : "Yetkili Çavuş")}\nİmza:\nKaşe:",
            "Ad Soyad: Ali SARIGÜL\nGörev: Kantar / Sevkiyat Amiri\nİmza:",
            "Onay Durumu: ONAYLANDI\nGörev: Muhasebe Müdürü\nİmza / Kaşe:"
        };

        for (int i = 0; i < 3; i++)
        {
            var sigRect = new Rectangle(x + (i * (sigW + 12)), y, sigW, sigH);
            g.FillRectangle(brushCardBg, sigRect);
            g.DrawRectangle(penBorder, sigRect);

            g.DrawString(sigTitles[i], fontKpiLbl, brushMuted, sigRect.X + 8, sigRect.Y + 8);
            g.DrawString(sigSub[i], fontSub, brushDark, sigRect.X + 8, sigRect.Y + 26);
        }
    }
}
