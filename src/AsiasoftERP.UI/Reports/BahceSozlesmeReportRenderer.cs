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
/// High-resolution vector A4 renderer for Orchard Purchase & Cutting Contracts
/// </summary>
public static class BahceSozlesmeReportRenderer
{
    public static void PrintReport(BahceSozlesmeDto sozlesme, List<BahceKesimSeferDto>? seferler = null)
    {
        var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
        pd.DefaultPageSettings.Landscape = false;

        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                Render(e.Graphics, e.MarginBounds, sozlesme, seferler ?? new List<BahceKesimSeferDto>());
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

    public static Bitmap RenderToBitmap(BahceSozlesmeDto sozlesme, List<BahceKesimSeferDto>? seferler = null, int width = 1240, int height = 1754)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(50, 50, width - 100, height - 100);
        Render(g, bounds, sozlesme, seferler ?? new List<BahceKesimSeferDto>());

        return bmp;
    }

    public static void Render(Graphics g, Rectangle bounds, BahceSozlesmeDto s, List<BahceKesimSeferDto> seferler)
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
        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var brushTeal = new SolidBrush(Color.FromArgb(13, 148, 136));
        using var brushGreen = new SolidBrush(Color.FromArgb(16, 120, 70));
        using var brushAmber = new SolidBrush(Color.FromArgb(180, 83, 9));

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

        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontHeader, brushDark, x + 72, y + 2);
        g.DrawString("Adres: Karaali Mah. Park Apt. No:4 Antakya / HATAY  •  Tel: +90 (326) 221 38 48", fontSub, brushMuted, x + 72, y + 24);
        g.DrawString("Vergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53", fontSub, brushMuted, x + 72, y + 39);

        // Document Badge (Right)
        int badgeW = 280;
        int badgeH = 58;
        var badgeRect = new Rectangle(x + w - badgeW, y + 2, badgeW, badgeH);
        using var brushBadgeBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penBadge = new Pen(Color.FromArgb(134, 239, 172), 1f);
        g.FillRectangle(brushBadgeBg, badgeRect);
        g.DrawRectangle(penBadge, badgeRect);

        using var fontDocTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var fontDocSub = new Font("Segoe UI", 8f);
        g.DrawString("BAHÇE ALIM VE KESİM SÖZLEŞMESİ", fontDocTitle, brushGreen, badgeRect.X + 12, badgeRect.Y + 6);
        g.DrawString($"Sözleşme No: #{s.SozlesmeNo:0}  ({s.SozlesmeTuru})", fontDocSub, brushDark, badgeRect.X + 12, badgeRect.Y + 26);
        g.DrawString($"Sözleşme Tarihi: {s.TarihFormatted}", fontDocSub, brushMuted, badgeRect.X + 12, badgeRect.Y + 41);

        y += headerHeight + 12;

        // 2. PRODUCER & PARCEL INFO CARDS (2 Columns)
        int cardH = 88;
        int halfW = (w - 12) / 2;

        // Left Card: Müstahsil / Üretici
        var cardLeft = new Rectangle(x, y, halfW, cardH);
        using var brushCardBg = new SolidBrush(Color.FromArgb(248, 250, 252));
        g.FillRectangle(brushCardBg, cardLeft);
        g.DrawRectangle(penBorder, cardLeft);

        g.DrawString("MÜSTAHSİL / BAHÇE SAHİBİ BİLGİLERİ", fontKpiLbl, brushMuted, cardLeft.X + 10, cardLeft.Y + 8);
        g.DrawString($"Müstahsil / Cari: {s.FirmaAdi}", fontBold, brushDark, cardLeft.X + 10, cardLeft.Y + 24);
        g.DrawString($"Bahçe Adı: {s.BahceAdi}", fontSub, brushDark, cardLeft.X + 10, cardLeft.Y + 44);
        g.DrawString($"Mevki / Köy: {s.Mevki}  •  Bölge: {s.BolgeAdi}", fontSub, brushMuted, cardLeft.X + 10, cardLeft.Y + 62);

        // Right Card: Tapu, Parsel & Ürün Şartları
        var cardRight = new Rectangle(x + halfW + 12, y, halfW, cardH);
        g.FillRectangle(brushCardBg, cardRight);
        g.DrawRectangle(penBorder, cardRight);

        g.DrawString("PARSEL VE HASAT ŞARTLARI", fontKpiLbl, brushMuted, cardRight.X + 10, cardRight.Y + 8);
        g.DrawString($"Ürün / Cins: {s.MalAdi}", fontBold, brushDark, cardRight.X + 10, cardRight.Y + 24);
        g.DrawString($"Ada / Parsel: {(string.IsNullOrEmpty(s.Ada) ? "-" : s.Ada)} / {(string.IsNullOrEmpty(s.Parsel) ? "-" : s.Parsel)}  •  Durumu: {s.Durumu}", fontSub, brushDark, cardRight.X + 10, cardRight.Y + 44);
        g.DrawString($"Sözleşme Modeli: {(s.SozlesmeTuru == "KABALA" ? "KABALA (Götürü Sabit Bedel)" : "TONAJ (Birim Fiyat Esaslı)")}", fontSub, brushAmber, cardRight.X + 10, cardRight.Y + 62);

        y += cardH + 12;

        // 3. REKOLTE & PARASAL ÖZET ŞERİDİ
        int barH = 50;
        var barRect = new Rectangle(x, y, w, barH);
        using var brushBarBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penBar = new Pen(Color.FromArgb(134, 239, 172), 1.5f);
        g.FillRectangle(brushBarBg, barRect);
        g.DrawRectangle(penBar, barRect);

        int kw = w / 4;
        // Kpi 1: Hedef Rekolte
        g.DrawString("HEDEF REKOLTE", fontKpiLbl, brushMuted, x + 12, y + 6);
        g.DrawString(s.HedefTonFormatted, fontKpiVal, brushDark, x + 12, y + 22);

        // Kpi 2: Birim / Kabala Fiyat
        g.DrawString("ALIM ŞARTI / BİRİM FİYAT", fontKpiLbl, brushMuted, x + kw + 12, y + 6);
        string fiyatStr = s.SozlesmeTuru == "KABALA" ? $"{s.KabalaFiyat:N0} TL (Götürü)" : $"{s.Fiyat:N2} TL / KG";
        g.DrawString(fiyatStr, fontKpiVal, brushTeal, x + kw + 12, y + 22);

        // Kpi 3: Gerçekleşen Kesim
        g.DrawString("GERÇEKLEŞEN KESİM", fontKpiLbl, brushMuted, x + (kw * 2) + 12, y + 6);
        g.DrawString($"{s.GerceklesenTonFormatted}  ({s.TamamlanmaFormatted})", fontKpiVal, brushGreen, x + (kw * 2) + 12, y + 22);

        // Kpi 4: Toplam Sözleşme / Maliyet Hacmi
        g.DrawString("TOPLAM MALİYET / HACİM", fontKpiLbl, brushMuted, x + (kw * 3) + 12, y + 6);
        string volStr = s.GerceklesenMaliyet > 0 ? $"{s.GerceklesenMaliyet:N0} TL" : s.SozlesmeTutariFormatted;
        g.DrawString(volStr, fontKpiVal, brushGreen, x + (kw * 3) + 12, y + 22);

        y += barH + 16;

        // 4. REALIZED CUTTING TRIPS TABLE (If exists)
        if (seferler.Count > 0)
        {
            g.DrawString($"GERÇEKLEŞEN KESİM SEFERLERİ ({seferler.Count} Sefer):", fontBold, brushDark, x, y);
            y += 18;

            int thH = 24;
            var thRect = new Rectangle(x, y, w, thH);
            using var brushTh = new SolidBrush(Color.FromArgb(241, 245, 249));
            g.FillRectangle(brushTh, thRect);
            g.DrawRectangle(penBorder, thRect);

            int[] colWidths = { 35, 75, 100, 130, 140, 150, 90, 110, 110, 90, 110 };
            string[] colTitles = { "#", "Tarih", "Plaka", "Şoför", "Hasat Çavuşu", "Ürün", "Net KG", "Mal Bedeli", "İşçilik/Servis", "KG Maliyet", "Genel Toplam" };
            StringAlignment[] colAlign = { 
                StringAlignment.Center, StringAlignment.Center, StringAlignment.Near, StringAlignment.Near, 
                StringAlignment.Near, StringAlignment.Near, StringAlignment.Far, StringAlignment.Far, 
                StringAlignment.Far, StringAlignment.Far, StringAlignment.Far 
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

            int rowH = 22;
            int maxRows = Math.Min(seferler.Count, 16);
            for (int i = 0; i < maxRows; i++)
            {
                var item = seferler[i];
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
                    item.TarihFormatted,
                    item.Plaka,
                    item.Sofor,
                    item.KesimCavusu,
                    item.MalAdi,
                    item.NetKg.ToString("N0"),
                    item.MalTutari.ToString("N0"),
                    (item.YevmiyeTutari + item.ServisTutari).ToString("N0"),
                    item.KgMaliyet.ToString("N2"),
                    item.GenelToplam.ToString("N0")
                };

                for (int col = 0; col < colWidths.Length; col++)
                {
                    var r = new Rectangle(curX + 3, y, colWidths[col] - 6, rowH);
                    using var sf = new StringFormat { Alignment = colAlign[col], LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                    Brush b = col == 10 ? brushGreen : (col == 6 ? brushTeal : brushDark);
                    Font f = col == 10 || col == 6 ? fontBold : fontSub;
                    g.DrawString(vals[col], f, b, r, sf);
                    curX += colWidths[col];
                }
                y += rowH;
            }

            y += 12;
        }

        // 5. CONTRACTUAL TERMS & NOTICE
        var pnlTerms = new Rectangle(x, y, w, 68);
        g.FillRectangle(brushCardBg, pnlTerms);
        g.DrawRectangle(penBorder, pnlTerms);

        g.DrawString("SÖZLEŞME GENEL HÜKÜMLERİ VE TAAHHÜT ŞARTLARI:", fontKpiLbl, brushMuted, pnlTerms.X + 8, pnlTerms.Y + 6);
        string termsText = "1. İşbu sözleşme konusu bahçede dalındaki ürünün hasatı, alıcı firmanın kontrolünde ziraat eksperleri ve görevlendirilen amele çavuşları marifetiyle yapılacaktır.\n" +
                           "2. Kantar tartımları Tek Asya Merkez Kantarı veya yetkili mobil kantar üzerinden yapılacak, kantar fişi resmi tartım esas alınacaktır.\n" +
                           "3. Ödemeler müstahsil makbuzu karşılığında stopaj ve borsa tescil kesintileri yapıldıktan sonra sözleşme vadesinde banka aracılığıyla ödenecektir.";
        g.DrawString(termsText, fontSub, brushDark, new Rectangle(pnlTerms.X + 8, pnlTerms.Y + 22, pnlTerms.Width - 16, 42));

        y += 82;

        // 6. SIGNATURE BOXES (3 Columns)
        int sigH = 80;
        int sigW = (w - 24) / 3;

        string[] sigTitles = {
            "MÜSTAHSİL / BAHÇE SAHİBİ (SATICI)",
            "ZİRAAT MÜHENDİSİ / EKSPER",
            "ŞİRKET YETKİLİSİ (ALICI ONAYI)"
        };
        string[] sigSub = {
            $"Ad Soyad: {s.FirmaAdi}\nTC / VKN:\nİmza:",
            "Ad Soyad: Ali SARIGÜL\nGörev: Ziraat & Satın Alma Sorumlusu\nİmza:",
            "Onay Durumu: ONAYLANDI\nGörev: Genel Müdürlük / Finans\nİmza / Kaşe:"
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
