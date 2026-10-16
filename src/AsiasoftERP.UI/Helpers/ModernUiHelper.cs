using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AsiasoftERP.UI.Helpers;

public static class ModernColors
{
    // Deep Slate Brand Palette
    public static readonly Color Slate900 = Color.FromArgb(15, 23, 42);      // #0F172A (Sidebar / Darkest)
    public static readonly Color Slate800 = Color.FromArgb(30, 41, 59);      // #1E293B (Header / Cards Dark)
    public static readonly Color Slate700 = Color.FromArgb(51, 65, 85);      // #334155 (Borders Dark)
    public static readonly Color Slate600 = Color.FromArgb(71, 85, 105);     // #475569 (Muted Text Dark)
    public static readonly Color Slate500 = Color.FromArgb(100, 116, 139);   // #64748B (Secondary Text)
    public static readonly Color Slate300 = Color.FromArgb(203, 213, 225);   // #CBD5E1 (Splitter / Subtle Border)
    public static readonly Color Slate200 = Color.FromArgb(226, 232, 240);   // #E2E8F0 (Borders Light)
    public static readonly Color Slate100 = Color.FromArgb(241, 245, 249);   // #F1F5F9 (Background Canvas)
    public static readonly Color Slate50  = Color.FromArgb(248, 250, 252);   // #F8FAFC (Card Background)

    // Accent Colors
    public static readonly Color Teal600   = Color.FromArgb(13, 148, 136);   // #0D9488 (Fresh Agriculture Teal)
    public static readonly Color Emerald500= Color.FromArgb(16, 185, 129);   // #10B981 (Success / Alacak)
    public static readonly Color Emerald600= Color.FromArgb(5, 150, 105);    // #059669 (Dark Emerald)
    public static readonly Color Emerald50 = Color.FromArgb(236, 253, 245);  // #ECFDF5 (Light Emerald)
    public static readonly Color Blue600   = Color.FromArgb(37, 99, 235);    // #2563EB (Primary Action Blue)
    public static readonly Color Blue50    = Color.FromArgb(239, 246, 255);   // #EFF6FF (Light Blue)
    public static readonly Color Indigo600 = Color.FromArgb(79, 70, 229);    // #4F46E5 (Accent Violet)
    public static readonly Color Amber500  = Color.FromArgb(245, 158, 11);   // #F59E0B (Citrus Orange / Warning)
    public static readonly Color Rose600   = Color.FromArgb(225, 29, 72);     // #E11D48 (Borc / Danger)
    public static readonly Color WhatsAppGreen = Color.FromArgb(37, 211, 102); // #25D366 (WhatsApp Bright Green)
    public static readonly Color WhatsAppDark  = Color.FromArgb(7, 94, 84);    // #075E54 (WhatsApp Teal/Dark Header)
}

public static class ModernUiHelper
{
    public static void ApplyModernGridStyle(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(235, 238, 245);
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToOrderColumns = true;
        grid.AllowUserToResizeColumns = true;

        // Header style
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ModernColors.Slate800,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        // Rows style
        grid.RowTemplate.Height = 32;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = ModernColors.Slate800,
            Font = new Font("Segoe UI", 9.5f),
            SelectionBackColor = Color.FromArgb(224, 242, 254),
            SelectionForeColor = ModernColors.Slate900,
            Padding = new Padding(8, 0, 8, 0)
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(249, 251, 254),
            ForeColor = ModernColors.Slate800,
            SelectionBackColor = Color.FromArgb(224, 242, 254),
            SelectionForeColor = ModernColors.Slate900
        };
    }

    public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        float r = radius;
        if (r <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
        path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
        path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// A modern card panel with subtle rounded corners, crisp border and clean background
/// </summary>
public class ModernCardPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 10;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = ModernColors.Slate200;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color CardBackground { get; set; } = Color.White;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TopAccentColor { get; set; } = Color.Transparent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TopAccentHeight { get; set; } = 0;

    public ModernCardPanel()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        this.BackColor = Color.Transparent;
        this.Padding = new Padding(16);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
        using var path = ModernUiHelper.GetRoundedPath(rect, CornerRadius);

        // Fill background
        using var brush = new SolidBrush(CardBackground);
        e.Graphics.FillPath(brush, path);

        // Top Accent line if set
        if (TopAccentHeight > 0 && TopAccentColor != Color.Transparent)
        {
            var accentRect = new Rectangle(0, 0, this.Width, TopAccentHeight);
            using var clipPath = ModernUiHelper.GetRoundedPath(rect, CornerRadius);
            e.Graphics.SetClip(clipPath);
            using var accentBrush = new SolidBrush(TopAccentColor);
            e.Graphics.FillRectangle(accentBrush, accentRect);
            e.Graphics.ResetClip();
        }

        // Draw Border
        using var pen = new Pen(BorderColor, 1f);
        e.Graphics.DrawPath(pen, path);

        base.OnPaint(e);
    }
}
