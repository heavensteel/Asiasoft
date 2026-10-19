using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

/// <summary>
/// Vektörel A4 Resmi Sipariş Formu ve Yükleme Emri Çıktı Motoru
/// </summary>
public static class SiparisReportRenderer
{
    private static readonly Color PrimaryBlue = Color.FromArgb(30, 58, 138); // Deep Navy
    private static readonly Color PrimaryGreen = Color.FromArgb(22, 101, 52); // Forest Green
    private static readonly Color BorderColor = Color.FromArgb(203, 213, 225); // Slate 300
    private static readonly Color HeaderBg = Color.FromArgb(241, 245, 249); // Slate 100
    private static readonly Color TextDark = Color.FromArgb(15, 23, 42); // Slate 900
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139); // Slate 500

    public static Bitmap RenderToBitmap(SiparisDto siparis, int dpi = 96)
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

        Render(g, siparis, 8.27f * 96f, 11.69f * 96f);

        return bmp;
    }

    public static void RenderToGraphics(Graphics g, SiparisDto siparis, Rectangle pageBounds)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        Render(g, siparis, pageBounds.Width, pageBounds.Height);
    }

    public static void Render(Graphics g, SiparisDto siparis, float pageWidth, float pageHeight)
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

        using var penBorder = new Pen(BorderColor, 1f);
        using var penThick = new Pen(PrimaryBlue, 1.5f);
        using var brushDark = new SolidBrush(TextDark);
        using var brushMuted = new SolidBrush(TextMuted);
        using var brushPrimary = new SolidBrush(PrimaryBlue);
        using var brushGreen = new SolidBrush(PrimaryGreen);

        // ================= 1. HEADER (Şirket Bilgileri & Sipariş Künyesi) =================
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

        // Sağ Üst Sipariş Kutusu
        float badgeWidth = 205f;
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

        string orderTypeTitle = siparis.SiparisYonu == SiparisYonu.Alinan ? "📦 SATIŞ SİPARİŞİ FORMU" : "📥 SATIN ALMA SİPARİŞİ";
        g.DrawString(orderTypeTitle, fontBold, brushPrimary, badgeRect.X + 8f, badgeRect.Y + 5f);
        g.DrawString($"Sipariş No: SP-{(int)siparis.FisNo}", fontBold, brushDark, badgeRect.X + 8f, badgeRect.Y + 19f);
        g.DrawString($"Tarih: {siparis.Tarih:dd.MM.yyyy}  |  Durum: {siparis.Durumu}", fontSmall, brushMuted, badgeRect.X + 8f, badgeRect.Y + 32f);

        y += 52f;
        g.DrawLine(penThick, margin, y, margin + contentWidth, y);
        y += 8f;

        // ================= 2. MÜŞTERİ VE LOJİSTİK BİLGİLERİ (İki Kolon) =================
        float colW = (contentWidth - 10f) / 2f;
        var box1 = new RectangleF(margin, y, colW, 68f);
        var box2 = new RectangleF(margin + colW + 10f, y, colW, 68f);

        using (var brushBoxBg = new SolidBrush(HeaderBg))
        {
            g.FillRectangle(brushBoxBg, box1);
            g.FillRectangle(brushBoxBg, box2);
        }
        g.DrawRectangle(penBorder, box1.X, box1.Y, box1.Width, box1.Height);
        g.DrawRectangle(penBorder, box2.X, box2.Y, box2.Width, box2.Height);

        // Kutu 1: Müşteri / Cari
        var sfClip = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        string cariHdr = siparis.SiparisYonu == SiparisYonu.Alinan ? "ALICI / MÜŞTERİ BİLGİLERİ" : "TEDARİKÇİ / SATICI BİLGİLERİ";
        g.DrawString(cariHdr, fontSmall, brushMuted, box1.X + 8f, box1.Y + 4f);
        g.DrawString(siparis.FirmaAdi, fontBold, brushDark, new RectangleF(box1.X + 8f, box1.Y + 18f, box1.Width - 16f, 16f), sfClip);
        string locStr = $"Ülke / Şehir: {siparis.Ulke} {(string.IsNullOrWhiteSpace(siparis.Sehir) ? "" : "/ " + siparis.Sehir)}";
        g.DrawString(locStr, fontRegular, brushDark, new RectangleF(box1.X + 8f, box1.Y + 34f, box1.Width - 16f, 15f), sfClip);
        g.DrawString($"Teslim Şekli: {siparis.SatisSekli}  |  Pozisyon: {siparis.Pozisyon}", fontRegular, brushDark, new RectangleF(box1.X + 8f, box1.Y + 49f, box1.Width - 16f, 15f), sfClip);

        // Kutu 2: Lojistik ve Sevk
        g.DrawString("LOJİSTİK VE SEVKİYAT DETAYLARI", fontSmall, brushMuted, box2.X + 8f, box2.Y + 4f);
        string plakaStr = $"Araç Plaka: {(string.IsNullOrWhiteSpace(siparis.AracPlaka) ? "Atanacak" : siparis.AracPlaka)} {(string.IsNullOrWhiteSpace(siparis.AracPlaka2) ? "" : "/ " + siparis.AracPlaka2)}";
        g.DrawString(plakaStr, fontBold, brushDark, new RectangleF(box2.X + 8f, box2.Y + 18f, box2.Width - 16f, 16f), sfClip);
        string soforStr = $"Sürücü: {(string.IsNullOrWhiteSpace(siparis.SoforAdi) ? "-" : siparis.SoforAdi)} {(string.IsNullOrWhiteSpace(siparis.SoforTelefon) ? "" : "(" + siparis.SoforTelefon + ")")}";
        g.DrawString(soforStr, fontRegular, brushDark, new RectangleF(box2.X + 8f, box2.Y + 34f, box2.Width - 16f, 15f), sfClip);
        string sevkDateStr = siparis.BaslamaTarihi.HasValue ? siparis.BaslamaTarihi.Value.ToString("dd.MM.yyyy") : siparis.Tarih.ToString("dd.MM.yyyy");
        g.DrawString($"Sevk / Yükleme Tarihi: {sevkDateStr}  |  Frigo: {siparis.FrigoDerece}", fontRegular, brushDark, new RectangleF(box2.X + 8f, box2.Y + 49f, box2.Width - 16f, 15f), sfClip);

        y += 76f;

        // ================= 3. TESLİMAT & GÜMRÜK ADRESİ (Eğer varsa) =================
        if (!string.IsNullOrWhiteSpace(siparis.BosaltmaAdresi) || !string.IsNullOrWhiteSpace(siparis.GumrukAdres))
        {
            var adrBox = new RectangleF(margin, y, contentWidth, 42f);
            using (var bAdr = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.FillRectangle(bAdr, adrBox);
            }
            g.DrawRectangle(penBorder, adrBox.X, adrBox.Y, adrBox.Width, adrBox.Height);

            float halfW = (contentWidth - 10f) / 2f;
            g.DrawString("GÜMRÜK / TRANSİT NOKTASI", fontSmall, brushMuted, adrBox.X + 6f, adrBox.Y + 3f);
            string gText = string.IsNullOrWhiteSpace(siparis.GumrukAdres) ? "-" : siparis.GumrukAdres;
            if (gText.Length > 90) gText = gText.Substring(0, 87) + "...";
            g.DrawString(gText, fontSmall, brushDark, new RectangleF(adrBox.X + 6f, adrBox.Y + 15f, halfW - 10f, 24f));

            g.DrawString("MÜŞTERİ BOŞALTMA / TESLİMAT ADRESİ", fontSmall, brushMuted, adrBox.X + halfW + 6f, adrBox.Y + 3f);
            string bText = string.IsNullOrWhiteSpace(siparis.BosaltmaAdresi) ? "-" : siparis.BosaltmaAdresi;
            if (bText.Length > 90) bText = bText.Substring(0, 87) + "...";
            g.DrawString(bText, fontSmall, brushDark, new RectangleF(adrBox.X + halfW + 6f, adrBox.Y + 15f, halfW - 10f, 24f));

            y += 48f;
        }

        // ================= 4. SİPARİŞ KALEMLERİ TABLOSU =================
        g.DrawString("SİPARİŞ EDİLEN ÜRÜN VE AMBALAJ KALEMLERİ", fontBold, brushPrimary, margin, y);
        y += 16f;

        float hRow = 18f;
        var tableRect = new RectangleF(margin, y, contentWidth, hRow);
        using (var brushTableHdr = new SolidBrush(PrimaryBlue))
        {
            g.FillRectangle(brushTableHdr, tableRect);
        }

        // Sütunlar: # (22), Kod (45), Ürün Adı & Boy (205), Kasa/Ambalaj (160), Palet (45), Kasa Adedi (55), Net KG (75), Fiyat (55), Tutar (contentWidth - ...)
        float cNo = 22f;
        float cKod = 45f;
        float cUrun = 200f;
        float cAmb = 155f;
        float cPalet = 45f;
        float cKasa = 55f;
        float cKg = 75f;
        float cFiyat = 60f;
        float cTutar = contentWidth - (cNo + cKod + cUrun + cAmb + cPalet + cKasa + cKg + cFiyat);

        using (var brushHdrText = new SolidBrush(Color.White))
        {
            float curX = margin;
            var sfR = new StringFormat { Alignment = StringAlignment.Far };
            g.DrawString("#", fontBold, brushHdrText, curX + 2f, y + 2f); curX += cNo;
            g.DrawString("Kod", fontBold, brushHdrText, curX + 2f, y + 2f); curX += cKod;
            g.DrawString("Ürün / Mahsul Cinsi & Kalibre", fontBold, brushHdrText, curX + 2f, y + 2f); curX += cUrun;
            g.DrawString("Ambalaj / Kasa Türü", fontBold, brushHdrText, curX + 2f, y + 2f); curX += cAmb;
            g.DrawString("Palet", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cPalet - 4f, hRow), sfR); curX += cPalet;
            g.DrawString("Kasa", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cKasa - 4f, hRow), sfR); curX += cKasa;
            g.DrawString("Net KG", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cKg - 4f, hRow), sfR); curX += cKg;
            g.DrawString("B.Fiyat", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cFiyat - 4f, hRow), sfR); curX += cFiyat;
            g.DrawString("Tutar (" + siparis.DovizTuru + ")", fontBold, brushHdrText, new RectangleF(curX, y + 2f, cTutar - 4f, hRow), sfR);
        }

        y += hRow;

        var details = siparis.Detaylar ?? new System.Collections.Generic.List<SiparisDetayDto>();
        int rowIdx = 1;
        float maxTableY = pageHeight - 160f;

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
            var sfRight = new StringFormat { Alignment = StringAlignment.Far };

            g.DrawString(rowIdx.ToString(), fontRegular, brushMuted, cx + 2f, y + 2f); cx += cNo;
            g.DrawString(d.MalKodu, fontRegular, brushMuted, cx + 2f, y + 2f); cx += cKod;

            var sfLeft = new StringFormat { Alignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            string urunDesc = d.MalAdi;
            if (!string.IsNullOrWhiteSpace(d.Boy)) urunDesc += $" - {d.Boy}";
            if (!string.IsNullOrWhiteSpace(d.Kalite)) urunDesc += $" ({d.Kalite})";
            g.DrawString(urunDesc, fontBold, brushDark, new RectangleF(cx + 2f, y + 2f, cUrun - 4f, hRow), sfLeft); cx += cUrun;

            string ambDesc = d.KutuAdi;
            if (!string.IsNullOrWhiteSpace(d.AmbAdi)) ambDesc += $" / {d.AmbAdi}";
            g.DrawString(ambDesc, fontRegular, brushDark, new RectangleF(cx + 2f, y + 2f, cAmb - 4f, hRow), sfLeft); cx += cAmb;

            g.DrawString(d.PaletSayisi.ToString("N0"), fontRegular, brushDark, new RectangleF(cx, y + 2f, cPalet - 4f, hRow), sfRight); cx += cPalet;
            g.DrawString(d.ParcaSayisi.ToString("N0"), fontRegular, brushDark, new RectangleF(cx, y + 2f, cKasa - 4f, hRow), sfRight); cx += cKasa;
            g.DrawString(d.NetKg > 0 ? $"{d.NetKg:N0} KG" : "-", fontBold, brushDark, new RectangleF(cx, y + 2f, cKg - 4f, hRow), sfRight); cx += cKg;
            g.DrawString(d.Fiyati.ToString("N2"), fontRegular, brushDark, new RectangleF(cx, y + 2f, cFiyat - 4f, hRow), sfRight); cx += cFiyat;
            g.DrawString($"{d.Tutari:N2} {d.DovizTuru}", fontBold, brushDark, new RectangleF(cx, y + 2f, cTutar - 4f, hRow), sfRight);

            y += hRow;
            rowIdx++;
        }

        if (details.Count == 0)
        {
            g.DrawString("Bu siparişe ait girilmiş ürün kalemi bulunmamaktadır.", fontRegular, brushMuted, margin + 8f, y + 4f);
            y += hRow + 8f;
        }

        g.DrawLine(penThick, margin, y, margin + contentWidth, y);
        y += 10f;

        // ================= 5. MİKTAR VE FİNANSAL TOPLAMLAR =================
        float sumBoxWidth = 260f;
        var sumBox = new RectangleF(margin + contentWidth - sumBoxWidth, y, sumBoxWidth, 75f);
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

        double totPalet = siparis.PaletToplam > 0 ? siparis.PaletToplam : details.Sum(d => d.PaletSayisi);
        double totKasa = siparis.KasaToplam > 0 ? siparis.KasaToplam : details.Sum(d => d.ParcaSayisi);
        double totKg = siparis.NetToplam > 0 ? siparis.NetToplam : details.Sum(d => d.NetKg);
        double totTutar = siparis.ToplamTutar > 0 ? siparis.ToplamTutar : details.Sum(d => d.Tutari);

        DrawSummaryLine("Toplam Palet Adedi:", $"{totPalet:N0} Palet", false, TextDark);
        DrawSummaryLine("Toplam Kasa Sayısı:", $"{totKasa:N0} Kasa", false, TextDark);
        DrawSummaryLine("Toplam Net Ağırlık:", $"{totKg:N0} KG", false, TextDark);

        using (var penNet = new Pen(BorderColor, 1f))
        {
            g.DrawLine(penNet, sumBox.X + 8f, sy - 1f, sumBox.Right - 8f, sy - 1f);
        }
        DrawSummaryLine("GENEL TOPLAM TUTAR:", $"{totTutar:N2} {siparis.DovizTuru}", true, PrimaryGreen);

        // Sol tarafa açıklama ve notlar
        var noteBox = new RectangleF(margin, y, contentWidth - sumBoxWidth - 12f, 75f);
        g.DrawString("SİPARİŞ ŞARTLARI VE TALİMATLAR", fontSmall, brushMuted, noteBox.X, noteBox.Y + 4f);
        string noteText = string.IsNullOrWhiteSpace(siparis.Aciklama)
            ? "Ürünler belirtilen kalibre, boy ve ambalaj standartlarına uygun olarak soğuk zincir bozulmadan sevk edilecektir. Belirtilen gümrük ve boşaltma adresi sevkiyat evraklarında aynen yer alacaktır."
            : siparis.Aciklama;
        g.DrawString(noteText, fontSmall, brushDark, new RectangleF(noteBox.X, noteBox.Y + 18f, noteBox.Width, 54f));

        y += 85f;

        // ================= 6. ÇİFT İMZA VE ONAY BLOĞU =================
        float signBoxW = (contentWidth - 14f) / 2f;
        var sign1 = new RectangleF(margin, y, signBoxW, 60f);
        var sign2 = new RectangleF(margin + signBoxW + 14f, y, signBoxW, 60f);

        g.DrawRectangle(penBorder, sign1.X, sign1.Y, sign1.Width, sign1.Height);
        g.DrawRectangle(penBorder, sign2.X, sign2.Y, sign2.Width, sign2.Height);

        string sign1Title = siparis.SiparisYonu == SiparisYonu.Alinan ? "SİPARİŞ VEREN MÜŞTERİ / ALICI ONAYI" : "SİPARİŞİ ALAN TEDARİKÇİ ONAYI";
        g.DrawString(sign1Title, fontSmall, brushMuted, sign1.X + 8f, sign1.Y + 6f);
        g.DrawString($"Yetkili: {(string.IsNullOrWhiteSpace(siparis.SiparisiVeren) ? siparis.FirmaAdi : siparis.SiparisiVeren)}", fontBold, brushDark, new RectangleF(sign1.X + 8f, sign1.Y + 20f, sign1.Width - 16f, 18f), sfClip);
        g.DrawString("Kaşe / İmza / Tarih:", fontSmall, brushMuted, sign1.X + 8f, sign1.Y + 42f);

        g.DrawString("TEK ASYA TARIM - SATIŞ VE İHRACAT DİREKTÖRLÜĞÜ", fontSmall, brushMuted, sign2.X + 8f, sign2.Y + 6f);
        g.DrawString("Yetkili: Satış ve Sevkiyat Departmanı", fontBold, brushDark, sign2.X + 8f, sign2.Y + 20f);
        g.DrawString("Yetkili Kaşe / İmza:", fontSmall, brushMuted, sign2.X + 8f, sign2.Y + 42f);
    }
}
