using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

/// <summary>
/// Vektörel A4 Tır Sefer Maliyet ve Hakediş Raporu Çıktı Motoru
/// </summary>
public static class SeferMaliyetReportRenderer
{
    private static readonly Color PrimaryBlue = Color.FromArgb(30, 58, 138); // Deep Navy
    private static readonly Color PrimaryGreen = Color.FromArgb(22, 101, 52); // Forest Green
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225); // Slate 300
    private static readonly Color HeaderBg = Color.FromArgb(241, 245, 249); // Slate 100
    private static readonly Color TextDark = Color.FromArgb(15, 23, 42); // Slate 900
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139); // Slate 500

    public static Bitmap RenderToBitmap(SeferFisDto sefer, int dpi = 96)
    {
        int width = (int)(8.27f * dpi);
        int height = (int)(11.69f * dpi);

        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);

        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        float scale = dpi / 96f;
        g.ScaleTransform(scale, scale);

        Render(g, sefer, 8.27f * 96f, 11.69f * 96f);

        return bmp;
    }

    public static void RenderToGraphics(Graphics g, SeferFisDto sefer, Rectangle pageBounds)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        Render(g, sefer, pageBounds.Width, pageBounds.Height);
    }

    public static void Render(Graphics g, SeferFisDto sefer, float pageWidth, float pageHeight)
    {
        float margin = 36f;
        float contentWidth = pageWidth - (margin * 2);
        float y = margin;

        using var fontCompanyTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var fontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 7.5f, FontStyle.Regular);
        using var fontBold = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 8f, FontStyle.Regular);
        using var fontSmall = new Font("Segoe UI", 7f, FontStyle.Regular);
        using var fontKpiVal = new Font("Segoe UI", 11f, FontStyle.Bold);

        using var penBorder = new Pen(BorderColor, 1f);
        using var penThick = new Pen(PrimaryBlue, 1.5f);
        using var brushDark = new SolidBrush(TextDark);
        using var brushMuted = new SolidBrush(TextMuted);
        using var brushPrimary = new SolidBrush(PrimaryBlue);
        using var brushGreen = new SolidBrush(PrimaryGreen);

        // ================= 1. HEADER (Şirket Bilgileri & Belge Künyesi) =================
        var logoRect = new RectangleF(margin, y, 42f, 42f);
        using (var logoBrush = new SolidBrush(PrimaryBlue))
        {
            g.FillEllipse(logoBrush, logoRect);
        }
        using (var logoTextFont = new Font("Segoe UI", 12f, FontStyle.Bold))
        using (var logoTextBrush = new SolidBrush(Color.White))
        {
            var sfLogo = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("TA", logoTextFont, logoTextBrush, logoRect, sfLogo);
        }

        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontCompanyTitle, brushPrimary, margin + 48f, y);
        g.DrawString("Karaali Mah. Park Apt. No:4 Antakya / HATAY  •  Tel: +90 (326) 221 38 48\nVergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53  •  Ticaret Sicil No: 14208", fontSub, brushMuted, margin + 48f, y + 18f);

        // Sağ Üst Belge Kutusu
        float badgeWidth = 190f;
        float badgeHeight = 46f;
        var badgeRect = new RectangleF(margin + contentWidth - badgeWidth, y, badgeWidth, badgeHeight);
        using (var bgBadge = new SolidBrush(Color.FromArgb(238, 242, 255)))
        {
            g.FillRectangle(bgBadge, badgeRect);
        }
        using (var penBadge = new Pen(PrimaryBlue, 1f))
        {
            g.DrawRectangle(penBadge, badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height);
        }

        g.DrawString("🚛 TIR SEFER & MALİYET RAPORU", fontBold, brushPrimary, badgeRect.X + 8f, badgeRect.Y + 5f);
        g.DrawString($"Sefer Fiş No: SF-{(int)sefer.FisNo}", fontBold, brushDark, badgeRect.X + 8f, badgeRect.Y + 19f);
        g.DrawString($"Tarih: {sefer.Tarih:dd.MM.yyyy}  |  Durum: {sefer.Durumu}", fontSmall, brushMuted, badgeRect.X + 8f, badgeRect.Y + 32f);

        y += 52f;
        g.DrawLine(penThick, margin, y, margin + contentWidth, y);
        y += 8f;

        // ================= 2. ARAÇ & GÜZERGAH BİLGİLERİ (İki Kolon) =================
        float colW = (contentWidth - 10f) / 2f;
        var box1 = new RectangleF(margin, y, colW, 64f);
        var box2 = new RectangleF(margin + colW + 10f, y, colW, 64f);

        using (var brushBoxBg = new SolidBrush(HeaderBg))
        {
            g.FillRectangle(brushBoxBg, box1);
            g.FillRectangle(brushBoxBg, box2);
        }
        g.DrawRectangle(penBorder, box1.X, box1.Y, box1.Width, box1.Height);
        g.DrawRectangle(penBorder, box2.X, box2.Y, box2.Width, box2.Height);

        // Kutu 1: Araç ve Şoför
        g.DrawString("ARAÇ VE PERSONEL BİLGİLERİ", fontSmall, brushMuted, box1.X + 8f, box1.Y + 4f);
        g.DrawString($"Çekici Plaka: {sefer.Plaka}", fontBold, brushDark, box1.X + 8f, box1.Y + 17f);
        g.DrawString($"Dorse / Termokin: {(string.IsNullOrWhiteSpace(sefer.DorsePlaka) ? "-" : sefer.DorsePlaka)}", fontRegular, brushDark, box1.X + 8f, box1.Y + 31f);
        g.DrawString($"Sürücü / Lojistik: {sefer.SurucuAdi}", fontRegular, brushDark, box1.X + 8f, box1.Y + 45f);

        // Kutu 2: Güzergah ve Yük
        g.DrawString("GÜZERGAH VE YÜK DETAYLARI", fontSmall, brushMuted, box2.X + 8f, box2.Y + 4f);
        g.DrawString($"Çıkış Noktası: {sefer.CikisNoktasi}  ➔  Varış: {sefer.VarisNoktasi}", fontBold, brushDark, box2.X + 8f, box2.Y + 17f);
        g.DrawString($"Teslimat Yeri: {(string.IsNullOrWhiteSpace(sefer.YuklemeBosaltmaYeri) ? sefer.VarisNoktasi : sefer.YuklemeBosaltmaYeri)}", fontRegular, brushDark, box2.X + 8f, box2.Y + 31f);
        g.DrawString($"Sefer Türü: {sefer.SeferTuru}  |  Yük Cinsi: {sefer.YukCinsi}", fontRegular, brushDark, box2.X + 8f, box2.Y + 45f);

        y += 72f;

        // ================= 3. KM & YAKIT (MAZOT) PERFORMANS KARTI =================
        var kmBox = new RectangleF(margin, y, contentWidth, 48f);
        using (var brushKmBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
        {
            g.FillRectangle(brushKmBg, kmBox);
        }
        g.DrawRectangle(penBorder, kmBox.X, kmBox.Y, kmBox.Width, kmBox.Height);

        float segW = contentWidth / 6f;
        void DrawKmMetric(string title, string val, float xOff, Color valColor)
        {
            g.DrawString(title, fontSmall, brushMuted, kmBox.X + xOff + 6f, kmBox.Y + 6f);
            using var bVal = new SolidBrush(valColor);
            g.DrawString(val, fontBold, bVal, kmBox.X + xOff + 6f, kmBox.Y + 22f);
        }

        DrawKmMetric("ÇIKIŞ KM", $"{sefer.CikisKm:N0}", 0, TextDark);
        DrawKmMetric("DÖNÜŞ KM", $"{sefer.DonusKm:N0}", segW, TextDark);
        DrawKmMetric("YAPILAN KM", $"{sefer.YapilanKm:N0} KM", segW * 2, PrimaryBlue);
        DrawKmMetric("ALINAN YAKIT", $"{sefer.ToplamAlinanLitre:N0} LT", segW * 3, TextDark);
        DrawKmMetric("TÜKETİLEN YAKIT", $"{sefer.TuketilenToplamLitre:N0} LT", segW * 4, TextDark);
        DrawKmMetric("100 KM TÜKETİM", $"{sefer.YuzKmTuketim:N2} LT/100", segW * 5, PrimaryGreen);

        y += 56f;

        // ================= 4. SEFER MASRAF DETAYLARI TABLOSU =================
        g.DrawString("SEFER MASRAF VE GİDER KALEMLERİ", fontBold, brushPrimary, margin, y);
        y += 16f;

        float hRow = 18f;
        var tableRect = new RectangleF(margin, y, contentWidth, hRow);
        using (var brushTableHdr = new SolidBrush(PrimaryBlue))
        {
            g.FillRectangle(brushTableHdr, tableRect);
        }

        // Sütun Genişlikleri: # (24), Tarih (65), Masraf Açıklaması (330), Birim (40), Döviz Tutar (70), TL Tutarı (contentWidth - ...)
        float cNo = 24f;
        float cTarih = 65f;
        float cMasraf = contentWidth - (cNo + cTarih + 40f + 70f + 85f);
        float cBirim = 40f;
        float cDoviz = 70f;
        float cTl = 85f;

        using (var brushHdrText = new SolidBrush(Color.White))
        {
            float curX = margin;
            g.DrawString("#", fontBold, brushHdrText, curX + 4f, y + 2f); curX += cNo;
            g.DrawString("Tarih", fontBold, brushHdrText, curX + 4f, y + 2f); curX += cTarih;
            g.DrawString("Masraf Kalemi / Hizmet Türü", fontBold, brushHdrText, curX + 4f, y + 2f); curX += cMasraf;
            g.DrawString("Birim", fontBold, brushHdrText, curX + 4f, y + 2f); curX += cBirim;
            var sfRight = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString("Döviz Tutar", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cDoviz - 6f, hRow), sfRight); curX += cDoviz;
            g.DrawString("TL Tutarı", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cTl - 6f, hRow), sfRight);
        }

        y += hRow;

        var details = sefer.Detaylar ?? new System.Collections.Generic.List<SeferDetayDto>();
        int rowIdx = 1;
        float maxTableY = pageHeight - 190f; // Finansal özet ve imza için yer bırak

        foreach (var d in details)
        {
            if (y + hRow > maxTableY) break;

            var rRect = new RectangleF(margin, y, contentWidth, hRow);
            if (rowIdx % 2 == 0)
            {
                using var bZebra = new SolidBrush(Color.FromArgb(248, 250, 252));
                g.FillRectangle(bZebra, rRect);
            }
            g.DrawLine(penBorder, margin, y + hRow, margin + contentWidth, y + hRow);

            float cx = margin;
            g.DrawString(rowIdx.ToString(), fontRegular, brushMuted, cx + 4f, y + 2f); cx += cNo;
            g.DrawString(d.Tarih.ToString("dd.MM.yyyy"), fontRegular, brushDark, cx + 4f, y + 2f); cx += cTarih;

            string desc = d.HizmetAdi;
            if (!string.IsNullOrWhiteSpace(d.Aciklama)) desc += $" ({d.Aciklama})";
            if (desc.Length > 52) desc = desc.Substring(0, 49) + "...";
            g.DrawString(desc, fontRegular, brushDark, cx + 4f, y + 2f); cx += cMasraf;

            g.DrawString(d.Birim, fontRegular, brushMuted, cx + 4f, y + 2f); cx += cBirim;

            var sfR = new StringFormat { Alignment = StringAlignment.Far };
            string dvzStr = d.DovizTutari > 0 ? d.DovizTutari.ToString("N2") : "-";
            g.DrawString(dvzStr, fontRegular, brushDark, new RectangleF(cx, y + 2f, cDoviz - 6f, hRow), sfR); cx += cDoviz;

            g.DrawString(d.Tutar.ToString("N2") + " TL", fontBold, brushDark, new RectangleF(cx, y + 2f, cTl - 6f, hRow), sfR);

            y += hRow;
            rowIdx++;
        }

        if (details.Count == 0)
        {
            g.DrawString("Bu sefere ait girilmiş masraf kalemi bulunmamaktadır.", fontRegular, brushMuted, margin + 8f, y + 4f);
            y += hRow + 8f;
        }

        g.DrawLine(penThick, margin, y, margin + contentWidth, y);
        y += 12f;

        // ================= 5. FİNANSAL KÂRLILIK VE MUTABAKAT ÖZETİ =================
        float sumBoxWidth = 260f;
        var sumBox = new RectangleF(margin + contentWidth - sumBoxWidth, y, sumBoxWidth, 90f);
        using (var brushSumBg = new SolidBrush(Color.FromArgb(248, 250, 252)))
        {
            g.FillRectangle(brushSumBg, sumBox);
        }
        g.DrawRectangle(penBorder, sumBox.X, sumBox.Y, sumBox.Width, sumBox.Height);

        float sy = sumBox.Y + 6f;
        void DrawSummaryLine(string label, string val, bool isBold = false, Color? valColor = null)
        {
            g.DrawString(label, isBold ? fontBold : fontRegular, brushDark, sumBox.X + 8f, sy);
            var sfRight = new StringFormat { Alignment = StringAlignment.Far };
            using var bVal = new SolidBrush(valColor ?? (isBold ? TextDark : TextMuted));
            g.DrawString(val, isBold ? fontBold : fontRegular, bVal, new RectangleF(sumBox.X, sy, sumBox.Width - 8f, 15f), sfRight);
            sy += 16f;
        }

        double totalMasraf = sefer.ToplamMasrafTl > 0 
            ? sefer.ToplamMasrafTl 
            : (sefer.Detaylar?.Sum(d => d.Tutar) ?? 0);
        double netKar = sefer.SeferBedeli - (totalMasraf + (sefer.PrimTutariTl > 0 ? sefer.PrimTutariTl : 0));

        DrawSummaryLine("Navlun (Taşıma Bedeli):", $"{sefer.SeferBedeli:N2} TL", true, PrimaryBlue);
        DrawSummaryLine("Toplam Sefer Masrafları:", $"(-) {totalMasraf:N2} TL", false, Color.Crimson);
        DrawSummaryLine("Verilen Şoför Avansı:", $"{sefer.AlinanTlAvans + sefer.AlinanDovizAvans:N2} TL", false, TextDark);
        DrawSummaryLine("Şoför Sefer Primi:", $"(-) {sefer.PrimTutariTl:N2} TL", false, TextDark);

        // Net Kâr / Zarar Çizgisi
        using (var penNet = new Pen(BorderColor, 1f))
        {
            g.DrawLine(penNet, sumBox.X + 8f, sy - 2f, sumBox.Right - 8f, sy - 2f);
        }
        var netColor = netKar >= 0 ? PrimaryGreen : Color.Crimson;
        DrawSummaryLine("NET SEFER KÂR / ZARARI:", $"{netKar:N2} TL", true, netColor);

        // Sol tarafa açıklama ve mutabakat notu
        var noteBox = new RectangleF(margin, y, contentWidth - sumBoxWidth - 12f, 90f);
        g.DrawString("SEFER VE MUTABAKAT NOTU", fontSmall, brushMuted, noteBox.X, noteBox.Y + 4f);
        string noteText = string.IsNullOrWhiteSpace(sefer.Aciklama)
            ? "İşbu sefer formu; araç KM sayaç göstergeleri, yakıt tüketimleri ve ekte sunulan resmi harcama fişleri doğrultusunda tanzim edilmiş olup firma yetkilisi ve sürücü tarafından karşılıklı teyit edilmiştir."
            : sefer.Aciklama;
        g.DrawString(noteText, fontSmall, brushDark, new RectangleF(noteBox.X, noteBox.Y + 18f, noteBox.Width, 68f));

        y += 102f;

        // ================= 6. ÇİFT İMZA VE ONAY BLOĞU =================
        float signBoxW = (contentWidth - 14f) / 2f;
        var sign1 = new RectangleF(margin, y, signBoxW, 60f);
        var sign2 = new RectangleF(margin + signBoxW + 14f, y, signBoxW, 60f);

        g.DrawRectangle(penBorder, sign1.X, sign1.Y, sign1.Width, sign1.Height);
        g.DrawRectangle(penBorder, sign2.X, sign2.Y, sign2.Width, sign2.Height);

        g.DrawString("SEFERİ YAPAN SÜRÜCÜ / TAŞERON", fontSmall, brushMuted, sign1.X + 8f, sign1.Y + 6f);
        g.DrawString($"Adı Soyadı: {sefer.SurucuAdi}", fontBold, brushDark, sign1.X + 8f, sign1.Y + 20f);
        g.DrawString("İmza: (Yukarıdaki hakedişi eksiksiz teslim aldım)", fontSmall, brushMuted, sign1.X + 8f, sign1.Y + 42f);

        g.DrawString("TEK ASYA TARIM - SEVKİYAT VE FİLO ONAYI", fontSmall, brushMuted, sign2.X + 8f, sign2.Y + 6f);
        g.DrawString("Yetkili: Sevkiyat ve Operasyon Müdürü", fontBold, brushDark, sign2.X + 8f, sign2.Y + 20f);
        g.DrawString("Yetkili Kaşe / İmza:", fontSmall, brushMuted, sign2.X + 8f, sign2.Y + 42f);
    }
}
