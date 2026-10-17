using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

/// <summary>
/// High-resolution vector A4 renderer for official Farmer/Producer Purchase Receipts (Müstahsil Makbuzu)
/// conforming strictly to V.U.K. Madde 235 and agricultural tax regulations.
/// </summary>
public static class MustahsilMakbuzReportRenderer
{
    public static void PrintReport(MustahsilMakbuzDto makbuz)
    {
        var pd = new PrintDocument();
        pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
        pd.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
        pd.DefaultPageSettings.Landscape = false;

        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                Render(e.Graphics, e.MarginBounds, makbuz);
            }
            e.HasMorePages = false;
        };

        using var dlg = new PrintPreviewDialog
        {
            Document = pd,
            Width = 1050,
            Height = 850,
            StartPosition = FormStartPosition.CenterScreen
        };
        dlg.ShowDialog();
    }

    public static Bitmap RenderToBitmap(MustahsilMakbuzDto makbuz, int width = 1240, int height = 1754)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(50, 50, width - 100, height - 100);
        Render(g, bounds, makbuz);

        return bmp;
    }

    public static void Render(Graphics g, Rectangle bounds, MustahsilMakbuzDto m)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        using var fontHeader = new Font("Segoe UI", 13f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 8.5f);
        using var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 8.5f);
        using var fontKpiVal = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var fontKpiLbl = new Font("Segoe UI", 7.5f, FontStyle.Bold);

        using var penBorder = new Pen(Color.FromArgb(203, 213, 225), 1f);
        using var penDark = new Pen(Color.FromArgb(51, 65, 85), 1.5f);
        using var brushDark = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var brushEmerald = new SolidBrush(Color.FromArgb(16, 120, 70));
        using var brushRed = new SolidBrush(Color.FromArgb(190, 18, 60));
        using var brushCardBg = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var brushWhite = new SolidBrush(Color.White);

        int x = bounds.Left;
        int y = bounds.Top;
        int w = bounds.Width;

        // 1. TOP HEADER & LOGO
        int headerHeight = 72;
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
            g.FillEllipse(brushCircle, x, y + 2, 60, 60);
            using var fontCircle = new Font("Segoe UI", 13f, FontStyle.Bold);
            using var brushW = new SolidBrush(Color.White);
            g.DrawString("TA", fontCircle, brushW, x + 16, y + 17);
        }

        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ İNŞ. TUR. VE TİC. LTD. ŞTİ.", fontHeader, brushDark, x + 72, y + 2);
        g.DrawString("Adres: Karaali Mah. Park Apt. No:4 Antakya / HATAY  •  Tel: +90 (326) 221 38 48", fontSub, brushMuted, x + 72, y + 25);
        g.DrawString("Vergi Dairesi: 23 TEMMUZ V.D.  •  VKN: 833 005 15 53  •  Ticaret Sicil No: 14208", fontSub, brushMuted, x + 72, y + 42);

        // Document Badge (Right Box)
        int badgeW = 280;
        int badgeH = 68;
        var badgeRect = new Rectangle(x + w - badgeW, y + 2, badgeW, badgeH);
        using var brushBadgeBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penBadge = new Pen(Color.FromArgb(134, 239, 172), 1.5f);
        g.FillRectangle(brushBadgeBg, badgeRect);
        g.DrawRectangle(penBadge, badgeRect);

        using var fontDocTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var fontDocSub = new Font("Segoe UI", 8.5f);
        g.DrawString("🌾 MÜSTAHSİL MAKBUZU", fontDocTitle, brushEmerald, badgeRect.X + 12, badgeRect.Y + 6);
        g.DrawString($"Makbuz No: {m.MakbuzNo}", fontBold, brushDark, badgeRect.X + 12, badgeRect.Y + 28);
        g.DrawString($"Tarih: {m.TarihFormatted}  •  V.U.K. Md. 235", fontDocSub, brushMuted, badgeRect.X + 12, badgeRect.Y + 48);

        y += headerHeight + 14;

        // 2. PRODUCER & BUYER INFO CARDS (2 Columns)
        int cardH = 110;
        int halfW = (w - 12) / 2;

        // Left Card: Müstahsil / Çiftçi
        var cardLeft = new Rectangle(x, y, halfW, cardH);
        g.FillRectangle(brushCardBg, cardLeft);
        g.DrawRectangle(penBorder, cardLeft);

        g.DrawString("MÜSTAHSİL / ÇİFTÇİ BİLGİLERİ (MALI TESLİM EDEN)", fontKpiLbl, brushMuted, cardLeft.X + 10, cardLeft.Y + 8);
        g.DrawString($"Adı Soyadı: {m.AdiSoyadi}", fontBold, brushDark, cardLeft.X + 10, cardLeft.Y + 26);
        g.DrawString($"T.C. Kimlik No: {m.TcNo}   •   Bağ-Kur No: {(string.IsNullOrEmpty(m.BagkurNo) ? "MUAFIYETLİ" : m.BagkurNo)}", fontSub, brushDark, cardLeft.X + 10, cardLeft.Y + 46);
        g.DrawString($"Baba Adı: {(string.IsNullOrEmpty(m.BabaAdi) ? "-" : m.BabaAdi)}   •   Doğum Yeri/Yılı: {(string.IsNullOrEmpty(m.DogumTarihiYeri) ? "-" : m.DogumTarihiYeri)}", fontSub, brushMuted, cardLeft.X + 10, cardLeft.Y + 66);
        g.DrawString($"Adresi: {m.Adresi}  ({m.Ilece} / {m.Sehir})", fontSub, brushDark, cardLeft.X + 10, cardLeft.Y + 86);

        // Right Card: Alıcı ve Sevk
        var cardRight = new Rectangle(x + halfW + 12, y, halfW, cardH);
        g.FillRectangle(brushCardBg, cardRight);
        g.DrawRectangle(penBorder, cardRight);

        g.DrawString("ALICI FİRMA VE SEVKİYAT BİLGİLERİ (MALI SATIN ALAN)", fontKpiLbl, brushMuted, cardRight.X + 10, cardRight.Y + 8);
        g.DrawString("Alıcı Ünvanı: TEK ASYA TARIM ÜRÜNLERİ LTD. ŞTİ.", fontBold, brushDark, cardRight.X + 10, cardRight.Y + 26);
        g.DrawString($"Alım Yeri / Bölgesi: {m.Bolgesi}", fontSub, brushDark, cardRight.X + 10, cardRight.Y + 46);
        g.DrawString($"HKS Künye No: {(string.IsNullOrEmpty(m.KunyeNo) ? "-" : m.KunyeNo)}", fontSub, brushEmerald, cardRight.X + 10, cardRight.Y + 66);
        g.DrawString($"İşyeri / Tesis: MERKEZ İŞLETME   •   Kantar Giriş Ref: #{(m.GirisKodu > 0 ? m.GirisKodu.ToString("0") : "-")}", fontSub, brushMuted, cardRight.X + 10, cardRight.Y + 86);

        y += cardH + 14;

        // 3. TABLE: PURCHASED AGRICULTURAL GOODS
        int thH = 28;
        var thRect = new Rectangle(x, y, w, thH);
        using var brushTh = new SolidBrush(Color.FromArgb(241, 245, 249));
        g.FillRectangle(brushTh, thRect);
        g.DrawRectangle(penBorder, thRect);

        int[] colWidths = { 50, 110, 360, 150, 140, 130, 200 };
        string[] colTitles = { "#", "Mal Kodu", "Satın Alınan Mahsulün Cinsi & Nev'i", "HKS Künye", "Miktar (KG)", "Birim Fiyat", "Brüt Mal Bedeli" };
        StringAlignment[] colAlign = { StringAlignment.Center, StringAlignment.Center, StringAlignment.Near, StringAlignment.Center, StringAlignment.Far, StringAlignment.Far, StringAlignment.Far };

        int curX = x;
        for (int i = 0; i < colWidths.Length; i++)
        {
            var r = new Rectangle(curX + 4, y + 4, colWidths[i] - 8, thH - 8);
            using var sf = new StringFormat { Alignment = colAlign[i], LineAlignment = StringAlignment.Center };
            g.DrawString(colTitles[i], fontKpiLbl, brushDark, r, sf);
            curX += colWidths[i];
        }
        y += thH;

        // Data Row
        int rowH = 34;
        var rowRect = new Rectangle(x, y, w, rowH);
        g.DrawRectangle(penBorder, rowRect);

        string[] rowVals = {
            "1",
            string.IsNullOrEmpty(m.MalKodu) ? "1102" : m.MalKodu,
            m.MalAdi,
            string.IsNullOrEmpty(m.KunyeNo) ? "-" : m.KunyeNo,
            m.MiktarFormatted,
            m.FiyatFormatted,
            m.BrutTutarFormatted
        };

        curX = x;
        for (int col = 0; col < colWidths.Length; col++)
        {
            var r = new Rectangle(curX + 6, y + 6, colWidths[col] - 12, rowH - 12);
            using var sf = new StringFormat { Alignment = colAlign[col], LineAlignment = StringAlignment.Center };
            Brush b = col == 6 ? brushEmerald : brushDark;
            Font f = col == 6 || col == 4 ? fontBold : fontRegular;
            g.DrawString(rowVals[col], f, b, r, sf);
            curX += colWidths[col];
        }
        y += rowH + 16;

        // 4. FINANCIAL SUMMARY & TAX WITHHOLDING BREAKDOWN (2 Columns)
        int summaryH = 175;
        int leftNotesW = w - 460;
        int rightBoxW = 445;

        // Left Side: Yazı İle & Yasal Hükümler
        var leftNotesRect = new Rectangle(x, y, leftNotesW, summaryH);
        g.FillRectangle(brushCardBg, leftNotesRect);
        g.DrawRectangle(penBorder, leftNotesRect);

        g.DrawString("YAZI İLE ÖDENEN TUTAR:", fontKpiLbl, brushMuted, leftNotesRect.X + 12, leftNotesRect.Y + 10);
        using var fontYazi = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        g.DrawString(m.Yaziile, fontYazi, brushDark, new Rectangle(leftNotesRect.X + 12, leftNotesRect.Y + 28, leftNotesW - 24, 40));

        g.DrawString("V.U.K. VE SGK YASAL BİLDİRİMİ:", fontKpiLbl, brushMuted, leftNotesRect.X + 12, leftNotesRect.Y + 74);
        string legalText = "1. İşbu Müstahsil Makbuzu, 213 sayılı Vergi Usul Kanunu'nun 235. maddesi hükümlerine göre birinci ve ikinci sınıf tüccarlar ile kazancı basit usulde tespit edilenlerin, gerçek usulde vergiye tabi olmayan çiftçilerden satın aldıkları zirai mahsuller için düzenlenmiştir.\n" +
                           "2. Gelir Vergisi Kanunu 94. maddesi gereğince zirai mahsul alım stopajı yapılmış ve muhtasar beyanname ile ödenecektir.\n" +
                           "3. 5510 sayılı Sosyal Sigortalar Kanunu Ek-4 maddesi uyarınca çiftçi tevkifatı (SGK/Bağ-Kur) kesilmiş ve SGK hesabına intikal ettirilecektir.";
        using var fontLegal = new Font("Segoe UI", 7.5f);
        g.DrawString(legalText, fontLegal, brushMuted, new Rectangle(leftNotesRect.X + 12, leftNotesRect.Y + 92, leftNotesW - 24, 76));

        // Right Side: Tax Withholding Table Box
        var rightBoxRect = new Rectangle(x + leftNotesW + 15, y, rightBoxW, summaryH);
        g.FillRectangle(brushWhite, rightBoxRect);
        g.DrawRectangle(penBorder, rightBoxRect);

        int sy = rightBoxRect.Y + 8;
        int sRightX = rightBoxRect.Right - 14;
        int sLabelX = rightBoxRect.Left + 14;

        void DrawSummaryRow(string label, string value, Brush valBrush, Font f, bool hasLineAfter = false)
        {
            g.DrawString(label, f, brushDark, sLabelX, sy);
            using var sf = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString(value, f, valBrush, sRightX, sy, sf);
            sy += 24;
            if (hasLineAfter)
            {
                g.DrawLine(penBorder, sLabelX, sy - 4, sRightX, sy - 4);
                sy += 4;
            }
        }

        DrawSummaryRow("Brüt Mal Bedeli:", m.BrutTutarFormatted, brushDark, fontBold);
        DrawSummaryRow($"(-) Stopaj Kesintisi (%{m.StopajOrani:0.0}):", $"-{m.StopajFormatted}", brushRed, fontRegular);
        DrawSummaryRow($"(-) SGK / Bağ-Kur Kesintisi (%{m.BagkurOrani:0.0}):", $"-{m.BagkurFormatted}", brushRed, fontRegular);
        DrawSummaryRow($"(-) Ticaret Borsası Tescil (%{m.TicBorOrani:0.0}):", $"-{m.BorsaFormatted}", brushRed, fontRegular, hasLineAfter: true);

        DrawSummaryRow("Toplam Kesintiler:", $"-{m.NetKesintiFormatted}", brushRed, fontBold);

        // Net Amount Highlight Box
        var netBox = new Rectangle(sLabelX - 6, sy + 2, rightBoxW - 16, 34);
        using var brushNetBg = new SolidBrush(Color.FromArgb(240, 253, 244));
        using var penNet = new Pen(Color.FromArgb(134, 239, 172), 1.5f);
        g.FillRectangle(brushNetBg, netBox);
        g.DrawRectangle(penNet, netBox);

        using var fontNetLbl = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var fontNetVal = new Font("Segoe UI", 12f, FontStyle.Bold);
        g.DrawString("NET ÖDENEN:", fontNetLbl, brushEmerald, netBox.X + 8, netBox.Y + 8);
        using var sfNet = new StringFormat { Alignment = StringAlignment.Far };
        g.DrawString(m.OdenenFormatted, fontNetVal, brushEmerald, netBox.Right - 8, netBox.Y + 7, sfNet);

        y += summaryH + 24;

        // 5. SIGNATURE & STAMP BLOCKS (2 Equal Columns)
        int sigH = 110;
        int sigW = (w - 16) / 2;

        // Left Signature: Çiftçi / Müstahsil
        var sigLeft = new Rectangle(x, y, sigW, sigH);
        g.FillRectangle(brushWhite, sigLeft);
        g.DrawRectangle(penBorder, sigLeft);

        g.DrawString("MALLARI TESLİM EDEN ÇİFTÇİ (MÜSTAHSİL)", fontKpiLbl, brushMuted, sigLeft.X + 12, sigLeft.Y + 10);
        g.DrawString($"Adı Soyadı: {m.AdiSoyadi}", fontBold, brushDark, sigLeft.X + 12, sigLeft.Y + 30);
        g.DrawString($"T.C. Kimlik No: {m.TcNo}", fontSub, brushDark, sigLeft.X + 12, sigLeft.Y + 48);
        g.DrawString("İmza: (Yukarıda yazılı net bedeli eksiksiz teslim aldım)", fontSub, brushMuted, sigLeft.X + 12, sigLeft.Y + 88);

        // Right Signature: Tek Asya A.Ş.
        var sigRight = new Rectangle(x + sigW + 16, y, sigW, sigH);
        g.FillRectangle(brushWhite, sigRight);
        g.DrawRectangle(penBorder, sigRight);

        g.DrawString("MALLARI SATIN ALAN VE TEVKİFATI YAPAN", fontKpiLbl, brushMuted, sigRight.X + 12, sigRight.Y + 10);
        g.DrawString("TEK ASYA TARIM ÜRÜNLERİ LTD. ŞTİ.", fontBold, brushDark, sigRight.X + 12, sigRight.Y + 30);
        g.DrawString("Onay: Satın Alma & Ziraat Eksperi", fontSub, brushDark, sigRight.X + 12, sigRight.Y + 48);
        g.DrawString("Yetkili Kaşe / İmza:", fontSub, brushMuted, sigRight.X + 12, sigRight.Y + 88);
    }
}
