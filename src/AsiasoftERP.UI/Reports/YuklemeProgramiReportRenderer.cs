using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.IO;
using AsiasoftERP.Core.Models;

namespace AsiasoftERP.UI.Reports;

public static class YuklemeProgramiReportRenderer
{
    private static Image? _cachedAntet;

    public static Image? GetAntetImage()
    {
        if (_cachedAntet != null) return _cachedAntet;

        try
        {
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "yukleme_antet.png");
            if (File.Exists(p1))
            {
                _cachedAntet = Image.FromFile(p1);
                return _cachedAntet;
            }

            string p2 = @"D:\AsiaSoft\src\AsiasoftERP.UI\Resources\Images\yukleme_antet.png";
            if (File.Exists(p2))
            {
                _cachedAntet = Image.FromFile(p2);
                return _cachedAntet;
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Renders the loading program sheet onto the specified graphics context, scaled to fit pageBounds.
    /// Uses 800 x 1130 reference A4 coordinate space.
    /// </summary>
    public static void Render(Graphics g, YuklemeProgramiDto prog, Rectangle pageBounds)
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // Fill background pure white
        using (var bgBrush = new SolidBrush(Color.White))
        {
            g.FillRectangle(bgBrush, pageBounds);
        }

        // Calculate scaling
        const float refWidth = 800f;
        const float refHeight = 1130f;

        float scaleX = pageBounds.Width / refWidth;
        float scaleY = pageBounds.Height / refHeight;
        float scale = Math.Min(scaleX, scaleY);

        float offsetX = pageBounds.X + (pageBounds.Width - refWidth * scale) / 2f;
        float offsetY = pageBounds.Y + (pageBounds.Height - refHeight * scale) / 2f;

        var state = g.Save();
        g.TranslateTransform(offsetX, offsetY);
        g.ScaleTransform(scale, scale);

        // Core Drawing in 800 x 1130 reference space
        DrawReportInternal(g, prog);

        g.Restore(state);
    }

    private static void DrawReportInternal(Graphics g, YuklemeProgramiDto prog)
    {
        using var pen1 = new Pen(Color.Black, 1f);
        using var pen15 = new Pen(Color.Black, 1.5f);
        using var pen2 = new Pen(Color.Black, 2f);
        using var brushBlack = new SolidBrush(Color.Black);
        using var brushWhite = new SolidBrush(Color.White);
        using var brushDark = new SolidBrush(Color.FromArgb(20, 20, 20));
        using var brushLightGray = new SolidBrush(Color.FromArgb(243, 244, 246));
        using var brushBorderGray = new SolidBrush(Color.FromArgb(209, 213, 219));

        using var fontTitle = new Font("Arial", 13f, FontStyle.Bold);
        using var fontSubTitle = new Font("Arial", 9.5f, FontStyle.Bold);
        using var fontColHeader = new Font("Arial", 11f, FontStyle.Bold);
        using var fontItemBold = new Font("Arial", 8.5f, FontStyle.Bold);
        using var fontItemRegular = new Font("Arial", 8f, FontStyle.Regular);
        using var fontRules = new Font("Arial", 7.8f, FontStyle.Bold);
        using var fontDates = new Font("Arial", 8.2f, FontStyle.Bold);
        using var fontFooter = new Font("Arial", 8.5f, FontStyle.Bold);
        using var fontBoxHeader = new Font("Arial", 7.5f, FontStyle.Bold);
        using var fontBoxUser = new Font("Arial", 8f, FontStyle.Regular);

        float left = 35f;
        float right = 765f;
        float width = right - left; // 730

        // 1. ANTET BANNER (Y: 18 to 118)
        var antetImg = GetAntetImage();
        if (antetImg != null)
        {
            // Draw antet image proportional inside width 730, height 96
            g.DrawImage(antetImg, new RectangleF(left, 16f, width, 96f));
        }
        else
        {
            // Fallback Vector Header
            g.DrawString("TEK ASYA TARIM GIDA İÇ VE DIŞ TİCARET A.Ş.", fontTitle, brushBlack, new PointF(left + 80, 20));
            g.DrawString("Kanatlı Mah. Atatürk Cad. No:63/A Antakya / HATAY", fontItemRegular, brushBlack, new PointF(left + 80, 42));
            g.DrawString("Tel: 0 326 216 26 26 - Fax: 0 326 216 26 27 • Antakya V.D. 836 041 3326", fontItemRegular, brushBlack, new PointF(left + 80, 58));
        }

        // Horizontal Divider Line below Antet
        g.DrawLine(pen2, left, 116f, right, 116f);

        // 2. TITLE BAR & DATE BAR (Y: 124 to 158)
        // Left Black Box: [ YÜKLEME PROGRAMI ]
        float titleBoxWidth = 320f;
        float titleBoxHeight = 32f;
        var rectTitleBox = new RectangleF(left, 122f, titleBoxWidth, titleBoxHeight);
        g.FillRectangle(brushBlack, rectTitleBox);
        using (var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.DrawString("YÜKLEME PROGRAMI", fontTitle, brushWhite, rectTitleBox, sfCenter);
        }

        // Right Box: Tarih & Program No
        float dateBoxWidth = 240f;
        var rectDateBox = new RectangleF(right - dateBoxWidth, 122f, dateBoxWidth, titleBoxHeight);
        g.DrawRectangle(pen15, rectDateBox.X, rectDateBox.Y, rectDateBox.Width, rectDateBox.Height);
        using (var sfDate = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
        {
            string dateStr = $"TARİH : {prog.Tarih:dd.MM.yyyy}";
            string progNoStr = $"NO : {prog.ProgramNo}";
            g.DrawString(dateStr, fontSubTitle, brushBlack, new RectangleF(rectDateBox.X + 8, rectDateBox.Y + 2, rectDateBox.Width - 10, 15), sfDate);
            g.DrawString(progNoStr, fontDates, brushBlack, new RectangleF(rectDateBox.X + 8, rectDateBox.Y + 16, rectDateBox.Width - 10, 14), sfDate);
        }

        // 3. MAIN CONTENT COLUMNS BOX (Y: 162 to 985)
        float gridTop = 162f;
        float gridBottom = 985f;
        float gridHeight = gridBottom - gridTop; // 823px

        // Outer Grid Border
        g.DrawRectangle(pen2, left, gridTop, width, gridHeight);

        int rowCount = Math.Max(1, Math.Min(3, prog.SatirSayisi));
        int colCount = Math.Max(1, Math.Min(3, prog.SutunSayisi));
        int totalCells = rowCount * colCount;

        float rowHeight = gridHeight / rowCount;
        float colWidth = width / colCount;

        // Vertical dividers between columns
        for (int c = 1; c < colCount; c++)
        {
            float colX = left + c * colWidth;
            g.DrawLine(pen2, colX, gridTop, colX, gridBottom);
        }

        // Horizontal dividers between rows
        for (int r = 1; r < rowCount; r++)
        {
            float rowY = gridTop + r * rowHeight;
            g.DrawLine(pen2, left, rowY, right, rowY);
        }

        // Draw each grid cell (Row r, Col c)
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < colCount; c++)
            {
                int cellIndex = r * colCount + c + 1;
                var sutun = prog.Sutunlar.FirstOrDefault(s => (s.SatirNo == r + 1 && s.SutunNo == c + 1) || s.HucreIndex == cellIndex)
                            ?? prog.Sutunlar.ElementAtOrDefault(cellIndex - 1)
                            ?? new YuklemeSutunDto
                            {
                                SatirNo = r + 1,
                                SutunNo = c + 1,
                                HucreIndex = cellIndex,
                                Baslik = totalCells == 1 
                                    ? (string.IsNullOrWhiteSpace(prog.Baslik) ? "ROMANYA / METRO" : prog.Baslik) 
                                    : (rowCount == 1 ? $"SEVKİYAT {c + 1}" : $"BÖLÜM {cellIndex}")
                            };

                float cellX = left + c * colWidth;
                float cellY = gridTop + r * rowHeight;

                // A) Cell Destination Header (Black strip with white text)
                float colHeaderHeight = (rowCount == 1) ? 36f : (rowCount == 2 ? 28f : 24f);
                var rectColHeader = new RectangleF(cellX, cellY, colWidth, colHeaderHeight);
                g.FillRectangle(brushBlack, rectColHeader);
                g.DrawRectangle(pen1, rectColHeader.X, rectColHeader.Y, rectColHeader.Width, rectColHeader.Height);

                using (var sfColHeader = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    string headerTitle = !string.IsNullOrWhiteSpace(sutun.Baslik)
                        ? sutun.Baslik
                        : (!string.IsNullOrWhiteSpace(prog.Baslik) && totalCells == 1 ? prog.Baslik : $"BÖLÜM {cellIndex}");
                    
                    var fontH = (colCount == 1 && rowCount == 1) 
                        ? fontColHeader 
                        : (colCount == 3 || rowCount == 3 ? fontRules : fontSubTitle);
                    
                    g.DrawString(headerTitle.ToUpper(new System.Globalization.CultureInfo("tr-TR")), fontH, brushWhite, rectColHeader, sfColHeader);
                }

                // B) Cell Footer Box (Bottom of Cell: Total Pallets)
                float colFooterHeight = (rowCount == 1) ? 34f : (rowCount == 2 ? 26f : 22f);
                float colFooterY = cellY + rowHeight - colFooterHeight;
                g.DrawLine(pen2, cellX, colFooterY, cellX + colWidth, colFooterY);
                var rectColFooter = new RectangleF(cellX, colFooterY, colWidth, colFooterHeight);
                g.FillRectangle(brushLightGray, rectColFooter);

                using (var sfColTotal = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    int pCount = sutun.ToplamPalet > 0 ? sutun.ToplamPalet : CalculatePalletsFromText(sutun.UrunlerMetin);
                    string totalText = $"TOPLAM : {pCount} PLT";
                    var fontF = (rowCount == 3 || colCount == 3) ? fontBoxHeader : fontSubTitle;
                    g.DrawString(totalText, fontF, brushBlack, rectColFooter, sfColTotal);
                }

                // C) Cell Body Contents
                float contentY = cellY + colHeaderHeight;
                float padX = (colCount == 3) ? 5f : 8f;
                float availContentWidth = colWidth - padX * 2;
                float availBodyH = colFooterY - contentY;

                if (rowCount == 1)
                {
                    // Classic single-row layout
                    float sec1Y = contentY + 6f;
                    float sec1Height = (colCount == 1) ? 220f : 160f;
                    DrawProductsSection(g, sutun.UrunlerMetin, new RectangleF(cellX + padX, sec1Y, availContentWidth, sec1Height), fontItemBold, fontItemRegular, brushBlack);

                    float div1Y = sec1Y + sec1Height;
                    g.DrawLine(pen15, cellX, div1Y, cellX + colWidth, div1Y);

                    float sec2Y = div1Y + 6f;
                    float sec2Height = (colCount == 1) ? 310f : 340f;
                    DrawMultilineText(g, sutun.TalimatlarMetin, new RectangleF(cellX + padX, sec2Y, availContentWidth, sec2Height), fontRules, brushBlack, lineSpacing: 1.25f);

                    float div2Y = sec2Y + sec2Height;
                    g.DrawLine(pen15, cellX, div2Y, cellX + colWidth, div2Y);

                    float sec3Y = div2Y + 6f;
                    float sec3Height = (colCount == 1) ? 120f : 140f;
                    DrawMultilineText(g, sutun.PartilerMetin, new RectangleF(cellX + padX, sec3Y, availContentWidth, sec3Height), fontDates, brushBlack, lineSpacing: 1.25f);

                    if (!string.IsNullOrWhiteSpace(sutun.TarihlerMetin) && colCount == 1)
                    {
                        float div3Y = sec3Y + sec3Height;
                        g.DrawLine(pen15, cellX, div3Y, cellX + colWidth, div3Y);

                        float sec4Y = div3Y + 6f;
                        float sec4Height = colFooterY - sec4Y - 6f;
                        DrawMultilineText(g, sutun.TarihlerMetin, new RectangleF(cellX + padX, sec4Y, availContentWidth, sec4Height), fontDates, brushBlack, lineSpacing: 1.25f);
                    }
                }
                else
                {
                    // Multi-row grid layout (2x2, 2x3, 3x3)
                    float pRatio = (rowCount == 2) ? 0.44f : 0.40f;
                    float prodH = availBodyH * pRatio;

                    var fProdBold = (rowCount == 3 || colCount == 3) ? fontBoxHeader : fontItemBold;
                    var fProdReg = (rowCount == 3 || colCount == 3) ? fontBoxUser : fontItemRegular;
                    DrawProductsSection(g, sutun.UrunlerMetin, new RectangleF(cellX + padX, contentY + 3f, availContentWidth, prodH - 5f), fProdBold, fProdReg, brushBlack);

                    float divMidY = contentY + prodH;
                    g.DrawLine(pen15, cellX, divMidY, cellX + colWidth, divMidY);

                    var infoParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(sutun.PartilerMetin)) infoParts.Add(sutun.PartilerMetin.Trim());
                    if (!string.IsNullOrWhiteSpace(sutun.TalimatlarMetin)) infoParts.Add(sutun.TalimatlarMetin.Trim());
                    if (!string.IsNullOrWhiteSpace(sutun.TarihlerMetin)) infoParts.Add(sutun.TarihlerMetin.Trim());
                    string combinedInfo = string.Join("\r\n", infoParts);

                    var fInfo = (rowCount == 3 || colCount == 3) ? fontBoxUser : fontRules;
                    float infoH = colFooterY - (divMidY + 3f) - 2f;
                    DrawMultilineText(g, combinedInfo, new RectangleF(cellX + padX, divMidY + 3f, availContentWidth, infoH), fInfo, brushBlack, lineSpacing: 1.15f);
                }
            }
        }

        // 4. FOOTER / DAĞITILACAK BÖLÜMLER (Y: 994 to 1108)
        float footerLabelY = 992f;
        g.DrawString("NOT : DAĞITILACAK BÖLÜMLER", fontFooter, brushBlack, new PointF(left + 2, footerLabelY));

        float boxesTop = 1012f;
        float boxHeight = 88f;
        float gap = 8f;
        float numBoxes = 5f;
        float boxWidth = (width - gap * (numBoxes - 1)) / numBoxes;

        string[] boxTitles = new string[]
        {
            "HAZIRLAYAN",
            "İHRACAT BÖLÜMÜ",
            "NAKLİYE BÖLÜMÜ",
            "YÜKLEME SORUMLUSU",
            "İŞLETME ŞEFİ"
        };

        string[] boxSubtitles = new string[]
        {
            string.IsNullOrWhiteSpace(prog.Hazirlayan) ? "Ali SARIGÜL" : prog.Hazirlayan,
            prog.Onaylayan ?? "Onay",
            "Onay",
            "Onay",
            "Onay"
        };

        for (int b = 0; b < 5; b++)
        {
            float bx = left + b * (boxWidth + gap);
            var rectBox = new RectangleF(bx, boxesTop, boxWidth, boxHeight);

            // Box border
            g.DrawRectangle(pen15, rectBox.X, rectBox.Y, rectBox.Width, rectBox.Height);

            // Header strip of box (Height: 24)
            var rectBoxHdr = new RectangleF(bx, boxesTop, boxWidth, 24f);
            g.FillRectangle(brushLightGray, rectBoxHdr);
            g.DrawRectangle(pen1, rectBoxHdr.X, rectBoxHdr.Y, rectBoxHdr.Width, rectBoxHdr.Height);

            using (var sfBoxHdr = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(boxTitles[b], fontBoxHeader, brushBlack, rectBoxHdr, sfBoxHdr);
            }

            // Name or approval text inside box
            using (var sfBoxSub = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                var rectBoxSub = new RectangleF(bx + 2, boxesTop + 28f, boxWidth - 4, 20f);
                g.DrawString(boxSubtitles[b], fontBoxUser, brushBlack, rectBoxSub, sfBoxSub);
            }
        }
    }

    private static void DrawProductsSection(Graphics g, string text, RectangleF rect, Font fontBold, Font fontRegular, Brush brush)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        float y = rect.Y;
        float lineHeight = fontBold.GetHeight(g) + 2f;

        using var fontHighlight = new Font(fontBold.FontFamily, fontBold.Size + 0.5f, FontStyle.Bold);
        using var fontHighlightSmall = new Font(fontBold.FontFamily, fontBold.Size - 0.5f, FontStyle.Bold);

        foreach (var rawLine in lines)
        {
            if (y + lineHeight > rect.Bottom) break;

            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                y += lineHeight * 0.5f;
                continue;
            }

            bool isTitle = line.Contains("PLT") || line.Contains("PALET") || line.StartsWith("LİMON") || line.StartsWith("MAND") || line.StartsWith("NAR") || line.StartsWith("GREYF") || line.StartsWith("ELMA") || line.StartsWith("PORT");
            var f = isTitle ? fontHighlight : fontRegular;

            // Check if line exceeds column rect width
            var size = g.MeasureString(line, f);
            if (size.Width > rect.Width)
            {
                // Try smaller font first
                if (isTitle && g.MeasureString(line, fontHighlightSmall).Width <= rect.Width)
                {
                    f = fontHighlightSmall;
                }
                else
                {
                    // Word wrap
                    var words = line.Split(' ');
                    string currentLine = "";
                    foreach (var w in words)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? w : currentLine + " " + w;
                        if (g.MeasureString(testLine, f).Width > rect.Width)
                        {
                            g.DrawString(currentLine, f, brush, new PointF(isTitle ? rect.X : rect.X + 6, y));
                            y += lineHeight;
                            if (y + lineHeight > rect.Bottom) break;
                            currentLine = w;
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }
                    if (!string.IsNullOrEmpty(currentLine) && y + lineHeight <= rect.Bottom)
                    {
                        g.DrawString(currentLine, f, brush, new PointF(isTitle ? rect.X : rect.X + 6, y));
                        y += lineHeight;
                    }
                    continue;
                }
            }

            g.DrawString(line, f, brush, new PointF(isTitle ? rect.X : rect.X + 6, y));
            y += lineHeight;
        }
    }

    private static void DrawMultilineText(Graphics g, string text, RectangleF rect, Font font, Brush brush, float lineSpacing = 1.2f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        float y = rect.Y;
        float lineHeight = font.GetHeight(g) * lineSpacing;

        foreach (var rawLine in lines)
        {
            if (y + lineHeight > rect.Bottom) break;

            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                y += lineHeight * 0.6f;
                continue;
            }

            // Wrap text if wider than rect
            var size = g.MeasureString(line, font);
            if (size.Width <= rect.Width)
            {
                g.DrawString(line, font, brush, new PointF(rect.X, y));
                y += lineHeight;
            }
            else
            {
                // Word wrap
                var words = line.Split(' ');
                string currentLine = "";
                foreach (var w in words)
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? w : currentLine + " " + w;
                    if (g.MeasureString(testLine, font).Width > rect.Width)
                    {
                        g.DrawString(currentLine, font, brush, new PointF(rect.X, y));
                        y += lineHeight;
                        if (y + lineHeight > rect.Bottom) break;
                        currentLine = w;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }
                if (!string.IsNullOrEmpty(currentLine) && y + lineHeight <= rect.Bottom)
                {
                    g.DrawString(currentLine, font, brush, new PointF(rect.X, y));
                    y += lineHeight;
                }
            }
        }
    }

    public static int CalculatePalletsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        int total = 0;
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            int idx = line.IndexOf("PLT", StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                // Find numbers before PLT
                string before = line.Substring(0, idx).Trim();
                int colonIdx = before.LastIndexOf(':');
                if (colonIdx >= 0)
                {
                    before = before.Substring(colonIdx + 1).Trim();
                }
                var parts = before.Split(' ');
                string lastWord = parts[^1];
                if (int.TryParse(lastWord, out int p))
                {
                    total += p;
                }
            }
        }
        return total;
    }

    /// <summary>
    /// Renders the document to a high-resolution Bitmap.
    /// Default size 1200 x 1695 (approx 150 DPI A4).
    /// </summary>
    public static Bitmap RenderToBitmap(YuklemeProgramiDto prog, int width = 1200, int height = 1695)
    {
        var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            Render(g, prog, new Rectangle(0, 0, width, height));
        }
        return bmp;
    }

    /// <summary>
    /// Creates a standard .NET PrintDocument configured for A4 paper.
    /// </summary>
    public static PrintDocument CreatePrintDocument(YuklemeProgramiDto prog)
    {
        var doc = new PrintDocument();
        doc.DocumentName = $"Yukleme_Programi_{prog.ProgramNo}";
        doc.DefaultPageSettings.Landscape = false;
        doc.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

        doc.PrintPage += (s, e) =>
        {
            if (e.Graphics != null)
            {
                Render(e.Graphics, prog, e.MarginBounds);
            }
            e.HasMorePages = false;
        };

        return doc;
    }
}
