using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

public static class AlisIrsaliyeReportRenderer
{
    public static PrintDocument CreatePrintDocument(IrsaliyeStkDto hdr, List<IrsaliyeDetayStkDto> lines)
    {
        var doc = new PrintDocument();
        doc.DocumentName = $"Alis_Irsaliyesi_{hdr.BelgeNoDisplay}";
        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // A4 standard 100 dpi
        doc.DefaultPageSettings.Margins = new Margins(20, 20, 20, 20);

        doc.PrintPage += (s, e) =>
        {
            var bounds = e.MarginBounds;
            Render(e.Graphics!, hdr, lines, bounds);
            e.HasMorePages = false;
        };

        return doc;
    }

    public static Bitmap RenderToBitmap(IrsaliyeStkDto hdr, List<IrsaliyeDetayStkDto> lines, int width = 1240, int height = 1754)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        Render(g, hdr, lines, new Rectangle(30, 30, width - 60, height - 60));
        return bmp;
    }

    public static void Render(Graphics g, IrsaliyeStkDto hdr, List<IrsaliyeDetayStkDto> lines, Rectangle bounds)
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (hdr.ToplamMiktar <= 0 && lines.Count > 0)
        {
            hdr.ToplamPalet = lines.Sum(l => l.Palet);
            hdr.ToplamKasa = lines.Sum(l => l.Kasa);
            hdr.ToplamMiktar = lines.Sum(l => l.Miktar);
            hdr.FisTut = lines.Sum(l => l.Tutar);
            hdr.KdvTut = lines.Sum(l => l.KdvTut);
            hdr.GenTut = hdr.FisTut + hdr.KdvTut;
        }

        using var fontTitle = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontSection = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontGridHdr = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontGridRow = new Font("Segoe UI", 8f, FontStyle.Regular);
        using var fontGridRowBold = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var fontTotals = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        using var penBorder = new Pen(Color.FromArgb(130, 160, 150), 1.2f);
        using var penThin = new Pen(Color.FromArgb(200, 215, 210), 0.8f);
        using var brushDark = new SolidBrush(Color.FromArgb(20, 35, 30));
        using var brushSlate = new SolidBrush(Color.FromArgb(60, 80, 75));
        using var brushHdrBg = new SolidBrush(Color.FromArgb(240, 248, 245));
        using var brushAccentBg = new SolidBrush(Color.FromArgb(230, 246, 238));

        int x = bounds.X;
        int y = bounds.Y;
        int w = bounds.Width;

        // 1. TOP HEADER BANNER
        int headerH = 95;
        g.FillRectangle(brushHdrBg, x, y, w, headerH);
        g.DrawRectangle(penBorder, x, y, w, headerH);

        // Logo if available
        int textLeft = x + 15;
        try
        {
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "tekasya.jpg");
            if (!File.Exists(imgPath))
                imgPath = @"D:\AsiaSoft\src\AsiasoftERP.UI\Resources\Images\tekasya.jpg";
            if (File.Exists(imgPath))
            {
                using var logoImg = Image.FromFile(imgPath);
                g.DrawImage(logoImg, x + 10, y + 10, 75, 75);
                textLeft = x + 95;
            }
        }
        catch { }

        // Company Details
        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontTitle, brushDark, textLeft, y + 8);
        g.DrawString("Adres: Karaali Mah. Park Apt. No: 4 Antakya / HATAY\nTel: +90 (326) 221 38 48  •  Faks: +90 (326) 221 38 58\nVergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53  •  Mersis: 0833005155300016", fontSub, brushSlate, textLeft, y + 36);

        // Document Box on the Right
        int docBoxW = 240;
        int docBoxX = x + w - docBoxW;
        g.FillRectangle(brushAccentBg, docBoxX, y, docBoxW, headerH);
        g.DrawRectangle(penBorder, docBoxX, y, docBoxW, headerH);

        using var fontDocTitle = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        using var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("MAL KABUL & ALIŞ İRSALİYESİ", fontDocTitle, new SolidBrush(Color.FromArgb(16, 120, 70)), docBoxX + docBoxW / 2, y + 8, sfCenter);

        using var sfLeft = new StringFormat { Alignment = StringAlignment.Near };
        int docDetailsY = y + 34;
        g.DrawString($"Fiş / İrs No   : #{hdr.FisNo:N0}  /  {hdr.IrsNo:N0}", fontGridHdr, brushDark, docBoxX + 10, docDetailsY);
        string gibStr = !string.IsNullOrWhiteSpace(hdr.GibIrsNo) ? hdr.GibIrsNo : "(Belirtilmedi)";
        g.DrawString($"Tedarikçi İrs : {gibStr}", fontGridRowBold, brushDark, docBoxX + 10, docDetailsY + 18);
        g.DrawString($"Kabul Tarihi   : {hdr.TarihFormatted} {hdr.SevkSaatFormatted}", fontGridRow, brushDark, docBoxX + 10, docDetailsY + 34);

        y += headerH + 8;

        // 2. PARTIES & LOGISTICS SECTION (2 BOXES)
        int infoH = 110;
        int boxW = (w - 10) / 2;

        // Box 1: Supplier Info
        g.FillRectangle(brushHdrBg, x, y, boxW, 24);
        g.DrawRectangle(penBorder, x, y, boxW, infoH);
        g.DrawString("TEDARİKÇİ (SATICI FİRMA BİLGİLERİ)", fontSection, brushDark, x + 8, y + 4);

        int cy = y + 28;
        g.DrawString($"Tedarikçi : {hdr.FirmaAdi}", fontGridRowBold, brushDark, new RectangleF(x + 8, cy, boxW - 16, 32));
        cy += 32;
        string adr = !string.IsNullOrWhiteSpace(hdr.FirmaAdresi) ? hdr.FirmaAdresi : $"{hdr.Ilce} / {hdr.Sehir}";
        g.DrawString($"Adres     : {adr}", fontGridRow, brushSlate, new RectangleF(x + 8, cy, boxW - 16, 28));
        cy += 28;
        g.DrawString($"V.D. / VKN: {hdr.VergiDairesi}  -  {hdr.VergiNumarasi}", fontGridRow, brushDark, x + 8, cy);

        // Box 2: Transport & Vehicle Info
        int box2X = x + boxW + 10;
        g.FillRectangle(brushHdrBg, box2X, y, boxW, 24);
        g.DrawRectangle(penBorder, box2X, y, boxW, infoH);
        g.DrawString("NAKLİYE VE MAL KABUL BİLGİLERİ", fontSection, brushDark, box2X + 8, y + 4);

        int ty = y + 28;
        string plakaStr = !string.IsNullOrWhiteSpace(hdr.ArkaPlaka) ? $"{hdr.Plaka} / {hdr.ArkaPlaka}" : hdr.Plaka;
        g.DrawString($"Getiren Araç Plaka   : {plakaStr}", fontGridRowBold, brushDark, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Sürücü (Şoför)       : {hdr.SoforTamAdi} (TC: {hdr.SoforTc})", fontGridRow, brushDark, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Sürücü Telefon       : {hdr.SoforTelefon}", fontGridRow, brushSlate, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Taşıyıcı / Lojistik  : {hdr.TasiyiciFirma} (VKN: {hdr.TasiyiciFirmaVkn})", fontGridRow, brushSlate, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Teslim Deposu / Not  : {hdr.DepoAdi}  •  {hdr.Aciklama}", fontGridRow, brushDark, box2X + 8, ty);

        y += infoH + 8;

        // 3. GOODS TABLE (TESLİM ALINAN MALLAR / AMBALAJLAR)
        int tableY = y;
        int rowH = 22;

        // Column widths
        int[] cols = new int[] { 32, 85, 300, 70, 75, 80, 80, 95, 120 };
        string[] headers = new string[] { "#", "Stok Kodu", "Malzeme / Ürün Açıklaması", "Birim", "Kabul Miktar", "Birim Fiyat", "KDV Tutarı", "Tutar (TL)", "Açıklama / Not" };

        // Table Header
        g.FillRectangle(brushHdrBg, x, y, w, rowH);
        g.DrawRectangle(penBorder, x, y, w, rowH);

        int cx = x;
        using var sfRight = new StringFormat { Alignment = StringAlignment.Far };
        for (int i = 0; i < cols.Length; i++)
        {
            var rCell = new Rectangle(cx, y + 3, cols[i], rowH - 6);
            bool isNum = i == 0 || (i >= 4 && i <= 7);
            g.DrawString(headers[i], fontGridHdr, brushDark, rCell, isNum ? (i == 0 ? sfCenter : sfRight) : (i == 3 ? sfCenter : sfLeft));
            cx += cols[i];
            if (i < cols.Length - 1) g.DrawLine(penThin, cx, y, cx, y + rowH);
        }

        y += rowH;

        // Lines
        int maxRowsOnPage = 18;
        int displayCount = Math.Min(lines.Count, maxRowsOnPage);
        for (int r = 0; r < displayCount; r++)
        {
            var l = lines[r];
            if (r % 2 == 1)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(248, 252, 250)), x, y, w, rowH);
            }
            g.DrawRectangle(penThin, x, y, w, rowH);

            cx = x;
            DrawCell(g, cx, y, cols[0], (r + 1).ToString(), fontGridRow, brushSlate, sfCenter); cx += cols[0];
            DrawCell(g, cx, y, cols[1], l.MalKodu, fontGridRow, brushDark, sfCenter); cx += cols[1];
            DrawCell(g, cx, y, cols[2], l.MalAdi, fontGridRowBold, brushDark, sfLeft); cx += cols[2];
            DrawCell(g, cx, y, cols[3], l.Birimi, fontGridRow, brushSlate, sfCenter); cx += cols[3];
            DrawCell(g, cx, y, cols[4], l.Miktar.ToString("N2"), fontGridRowBold, brushDark, sfRight); cx += cols[4];
            DrawCell(g, cx, y, cols[5], l.Fiyati > 0 ? l.Fiyati.ToString("N2") : "-", fontGridRow, brushDark, sfRight); cx += cols[5];
            DrawCell(g, cx, y, cols[6], l.KdvTut > 0 ? l.KdvTut.ToString("N2") : "-", fontGridRow, brushSlate, sfRight); cx += cols[6];
            DrawCell(g, cx, y, cols[7], l.Tutar > 0 ? l.Tutar.ToString("N2") : "-", fontGridRowBold, brushDark, sfRight); cx += cols[7];
            DrawCell(g, cx, y, cols[8], l.Aciklama, fontGridRow, brushSlate, sfLeft);

            y += rowH;
        }

        // Fill remaining blank rows up to at least 10
        int blanks = Math.Max(0, 10 - displayCount);
        for (int b = 0; b < blanks; b++)
        {
            g.DrawRectangle(penThin, x, y, w, rowH);
            y += rowH;
        }

        y += 8;

        // 4. TOTALS & SIGNATURES AREA
        int summaryH = 120;
        int signBoxW = (w - 320) / 2;

        // Signatures (Teslim Eden / Teslim Alan)
        g.DrawRectangle(penBorder, x, y, signBoxW, summaryH);
        g.FillRectangle(brushHdrBg, x, y, signBoxW, 22);
        g.DrawString("MALI TESLİM EDEN (Tedarikçi / Sürücü)", fontSection, brushDark, x + 6, y + 4);
        g.DrawString($"Firma / Şoför: {hdr.SoforTamAdi}\nAraç Plaka: {hdr.Plaka}\nİmza:\n\nKaşe:", fontSub, brushSlate, x + 8, y + 30);

        int signBox2X = x + signBoxW + 8;
        g.DrawRectangle(penBorder, signBox2X, y, signBoxW, summaryH);
        g.FillRectangle(brushHdrBg, signBox2X, y, signBoxW, 22);
        g.DrawString("MAL KABUL SORUMLUSU (Depo Yetkilisi)", fontSection, brushDark, signBox2X + 6, y + 4);
        g.DrawString($"Kullanıcı: {hdr.User}\nDepo: {hdr.DepoAdi}\nİmza:\n\nKaşe:", fontSub, brushSlate, signBox2X + 8, y + 30);

        // Totals Card on Right
        int totX = x + w - 300;
        int totW = 300;
        g.FillRectangle(brushAccentBg, totX, y, totW, summaryH);
        g.DrawRectangle(penBorder, totX, y, totW, summaryH);

        int ty2 = y + 8;
        DrawTotalRow(g, totX, ty2, totW, "Toplam Kalem Sayısı:", $"{lines.Count} KALEM", fontGridHdr, brushDark, sfRight);
        ty2 += 18;
        DrawTotalRow(g, totX, ty2, totW, "Toplam Kabul Miktarı:", $"{hdr.ToplamMiktar:N2}", fontTotals, new SolidBrush(Color.FromArgb(16, 120, 70)), sfRight);
        ty2 += 20;
        g.DrawLine(penThin, totX + 10, ty2, totX + totW - 10, ty2);
        ty2 += 6;
        DrawTotalRow(g, totX, ty2, totW, "Mal Bedeli (Ara Toplam):", $"{hdr.FisTut:N2} TL", fontGridRow, brushDark, sfRight);
        ty2 += 16;
        DrawTotalRow(g, totX, ty2, totW, "Hesaplanan KDV:", $"{hdr.KdvTut:N2} TL", fontGridRow, brushDark, sfRight);
        ty2 += 18;
        DrawTotalRow(g, totX, ty2, totW, "GENEL TOPLAM:", $"{hdr.GenTut:N2} TL", fontTotals, new SolidBrush(Color.FromArgb(180, 20, 20)), sfRight);

        // Footer Legal Note
        y += summaryH + 8;
        g.DrawString("İşbu mal kabul ve alış irsaliyesi formu ile sevk edilen malzemeler depo yetkililerince kalite, ambalaj ve miktar kontrolleri yapılarak teslim alınmıştır.", fontSub, brushSlate, x, y);
    }

    private static void DrawCell(Graphics g, int x, int y, int w, string text, Font font, Brush brush, StringFormat sf)
    {
        var r = new Rectangle(x + 4, y + 3, w - 8, 16);
        g.DrawString(text, font, brush, r, sf);
    }

    private static void DrawTotalRow(Graphics g, int x, int y, int w, string label, string value, Font font, Brush brush, StringFormat sfRight)
    {
        g.DrawString(label, font, brush, x + 10, y);
        g.DrawString(value, font, brush, new Rectangle(x, y, w - 12, 18), sfRight);
    }
}
