using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

public static class SatisIrsaliyeReportRenderer
{
    public static PrintDocument CreatePrintDocument(IrsaliyeStkDto hdr, List<IrsaliyeDetayStkDto> lines)
    {
        var doc = new PrintDocument();
        doc.DocumentName = $"Sevk_Irsaliyesi_{hdr.BelgeNoDisplay}";
        doc.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169); // 100 dpi standard A4
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

        using var fontTitle = new Font("Segoe UI", 14f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var fontSection = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontGridHdr = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var fontGridRow = new Font("Segoe UI", 8f, FontStyle.Regular);
        using var fontGridRowBold = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var fontTotals = new Font("Segoe UI", 9.5f, FontStyle.Bold);

        using var penBorder = new Pen(Color.FromArgb(140, 150, 165), 1.2f);
        using var penThin = new Pen(Color.FromArgb(200, 210, 220), 0.8f);
        using var brushDark = new SolidBrush(Color.FromArgb(20, 30, 45));
        using var brushSlate = new SolidBrush(Color.FromArgb(70, 80, 95));
        using var brushHdrBg = new SolidBrush(Color.FromArgb(240, 244, 250));
        using var brushAccentBg = new SolidBrush(Color.FromArgb(226, 238, 255));

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
        int docBoxW = 220;
        int docBoxX = x + w - docBoxW;
        g.FillRectangle(brushAccentBg, docBoxX, y, docBoxW, headerH);
        g.DrawRectangle(penBorder, docBoxX, y, docBoxW, headerH);

        using var fontDocTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("SEVK İRSALİYESİ", fontDocTitle, new SolidBrush(Color.FromArgb(15, 60, 130)), docBoxX + docBoxW / 2, y + 8, sfCenter);

        using var sfLeft = new StringFormat { Alignment = StringAlignment.Near };
        int docDetailsY = y + 36;
        g.DrawString($"İrsaliye No : {hdr.BelgeNoDisplay}", fontGridHdr, brushDark, docBoxX + 10, docDetailsY);
        g.DrawString($"Düzenleme  : {hdr.FisTarFormatted} {hdr.SevkSaatFormatted}", fontGridRow, brushDark, docBoxX + 10, docDetailsY + 18);
        g.DrawString($"Fiili Sevk : {hdr.IrsTar?.ToString("dd.MM.yyyy") ?? hdr.FisTarFormatted}", fontGridRow, brushDark, docBoxX + 10, docDetailsY + 34);

        y += headerH + 8;

        // 2. PARTIES & LOGISTICS SECTION (2 BOXES)
        int infoH = 110;
        int boxW = (w - 10) / 2;

        // Box 1: Customer Info
        g.FillRectangle(brushHdrBg, x, y, boxW, 24);
        g.DrawRectangle(penBorder, x, y, boxW, infoH);
        g.DrawString("ALICI (MÜŞTERİ BİLGİLERİ)", fontSection, brushDark, x + 8, y + 4);

        int cy = y + 28;
        g.DrawString($"Ünvan : {hdr.FirmaAdi}", fontGridRowBold, brushDark, new RectangleF(x + 8, cy, boxW - 16, 32));
        cy += 32;
        string adr = !string.IsNullOrWhiteSpace(hdr.FirmaAdresi) ? hdr.FirmaAdresi : $"{hdr.Ilce} / {hdr.Sehir}";
        g.DrawString($"Adres : {adr}", fontGridRow, brushSlate, new RectangleF(x + 8, cy, boxW - 16, 28));
        cy += 28;
        g.DrawString($"V.D. / VKN : {hdr.VergiDairesi}  -  {hdr.VergiNumarasi}", fontGridRow, brushDark, x + 8, cy);

        // Box 2: Transport & Vehicle Info
        int box2X = x + boxW + 10;
        g.FillRectangle(brushHdrBg, box2X, y, boxW, 24);
        g.DrawRectangle(penBorder, box2X, y, boxW, infoH);
        g.DrawString("SEVKİYAT VE TAŞIMA BİLGİLERİ", fontSection, brushDark, box2X + 8, y + 4);

        int ty = y + 28;
        string plakaStr = !string.IsNullOrWhiteSpace(hdr.ArkaPlaka) ? $"{hdr.Plaka} / {hdr.ArkaPlaka}" : hdr.Plaka;
        g.DrawString($"Çekici / Dorse Plaka : {plakaStr}", fontGridRowBold, brushDark, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Sürücü (Şoför)       : {hdr.SoforTamAdi} (TC: {hdr.SoforTc})", fontGridRow, brushDark, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Sürücü Telefon       : {hdr.SoforTelefon}", fontGridRow, brushSlate, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Taşıyıcı Firma       : {hdr.TasiyiciFirma} (VKN: {hdr.TasiyiciFirmaVkn})", fontGridRow, brushSlate, box2X + 8, ty);
        ty += 18;
        g.DrawString($"Çıkış Deposu / Sip No: {hdr.DepoAdi}  •  Sip: {hdr.SiparisNo}", fontGridRow, brushDark, box2X + 8, ty);

        y += infoH + 8;

        // 3. GOODS TABLE (SEVK EDİLEN MALLAR)
        int tableY = y;
        int rowH = 22;

        // Column widths
        int[] cols = new int[] { 32, 75, 230, 48, 50, 65, 60, 75, 45, 65, 80, 110 };
        string[] headers = new string[] { "#", "Stok Kodu", "Malın Cinsi ve Açıklaması", "Palet", "Kasa", "Brüt Kg", "Dara Kg", "Net Miktar", "Birim", "Birim Fiyat", "Tutar (TL)", "HKS Künye No" };

        // Table Header
        g.FillRectangle(brushHdrBg, x, y, w, rowH);
        g.DrawRectangle(penBorder, x, y, w, rowH);

        int cx = x;
        using var sfRight = new StringFormat { Alignment = StringAlignment.Far };
        for (int i = 0; i < cols.Length; i++)
        {
            var rCell = new Rectangle(cx, y + 3, cols[i], rowH - 6);
            bool isNum = i == 0 || i >= 3 && i <= 10;
            g.DrawString(headers[i], fontGridHdr, brushDark, rCell, isNum ? (i == 0 || i == 8 ? sfCenter : sfRight) : sfLeft);
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
                g.FillRectangle(new SolidBrush(Color.FromArgb(250, 252, 255)), x, y, w, rowH);
            }
            g.DrawRectangle(penThin, x, y, w, rowH);

            cx = x;
            DrawCell(g, cx, y, cols[0], (r + 1).ToString(), fontGridRow, brushSlate, sfCenter); cx += cols[0];
            DrawCell(g, cx, y, cols[1], l.MalKodu, fontGridRow, brushDark, sfCenter); cx += cols[1];
            DrawCell(g, cx, y, cols[2], l.MalAdi, fontGridRowBold, brushDark, sfLeft); cx += cols[2];
            DrawCell(g, cx, y, cols[3], l.Palet > 0 ? l.Palet.ToString("N0") : "-", fontGridRow, brushDark, sfRight); cx += cols[3];
            DrawCell(g, cx, y, cols[4], l.Kasa > 0 ? l.Kasa.ToString("N0") : "-", fontGridRow, brushDark, sfRight); cx += cols[4];
            DrawCell(g, cx, y, cols[5], l.Burut > 0 ? l.Burut.ToString("N1") : "-", fontGridRow, brushSlate, sfRight); cx += cols[5];
            DrawCell(g, cx, y, cols[6], l.Dara > 0 ? l.Dara.ToString("N1") : "-", fontGridRow, brushSlate, sfRight); cx += cols[6];
            DrawCell(g, cx, y, cols[7], l.Miktar.ToString("N2"), fontGridRowBold, brushDark, sfRight); cx += cols[7];
            DrawCell(g, cx, y, cols[8], l.Birimi, fontGridRow, brushSlate, sfCenter); cx += cols[8];
            DrawCell(g, cx, y, cols[9], l.Fiyati > 0 ? l.Fiyati.ToString("N2") : "-", fontGridRow, brushDark, sfRight); cx += cols[9];
            DrawCell(g, cx, y, cols[10], l.Tutar > 0 ? l.Tutar.ToString("N2") : "-", fontGridRowBold, brushDark, sfRight); cx += cols[10];
            DrawCell(g, cx, y, cols[11], l.KunyeNo, fontGridRow, brushSlate, sfLeft);

            y += rowH;
        }

        // Fill remaining blank rows up to at least 10 for official appearance
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
        g.DrawString("MALI TESLİM EDEN (Şirket Yetkilisi / Şoför)", fontSection, brushDark, x + 6, y + 4);
        g.DrawString($"Ad Soyad: {hdr.User}\nİmza:\n\nKaşe:", fontSub, brushSlate, x + 8, y + 30);

        int signBox2X = x + signBoxW + 8;
        g.DrawRectangle(penBorder, signBox2X, y, signBoxW, summaryH);
        g.FillRectangle(brushHdrBg, signBox2X, y, signBoxW, 22);
        g.DrawString("MALI TESLİM ALAN (Müşteri / Taşıyıcı)", fontSection, brushDark, signBox2X + 6, y + 4);
        g.DrawString("Ad Soyad:\nT.C. Kimlik No:\nİmza:\n\nKaşe:", fontSub, brushSlate, signBox2X + 8, y + 30);

        // Totals Card on Right
        int totX = x + w - 300;
        int totW = 300;
        g.FillRectangle(brushAccentBg, totX, y, totW, summaryH);
        g.DrawRectangle(penBorder, totX, y, totW, summaryH);

        int ty2 = y + 8;
        DrawTotalRow(g, totX, ty2, totW, "Toplam Palet Adedi:", $"{hdr.ToplamPalet:N0} PLT", fontGridHdr, brushDark, sfRight);
        ty2 += 18;
        DrawTotalRow(g, totX, ty2, totW, "Toplam Kasa Adedi:", $"{hdr.ToplamKasa:N0} KASA", fontGridHdr, brushDark, sfRight);
        ty2 += 18;
        DrawTotalRow(g, totX, ty2, totW, "Toplam Net Sevkiyat:", $"{hdr.ToplamMiktar:N2} KG", fontTotals, new SolidBrush(Color.FromArgb(15, 60, 130)), sfRight);
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
        g.DrawString("Bu sevk irsaliyesi 213 sayılı V.U.K. hükümlerine göre düzenlenmiştir. Malın teslimi sırasında alıcı tarafından kontrol edilerek imza karşılığı teslim alınmalıdır.", fontSub, brushSlate, x, y);
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
