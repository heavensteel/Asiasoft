using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class ToastNotificationForm : Form
{
    private static readonly List<ToastNotificationForm> _openToasts = new();

    public NotificationItem Item { get; }
    public event Action<NotificationItem>? ActionClicked;

    private System.Windows.Forms.Timer _dismissTimer = null!;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            cp.ExStyle |= 0x08000000;    // WS_EX_NOACTIVATE
            cp.ExStyle |= 0x00000080;    // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    public ToastNotificationForm(NotificationItem item)
    {
        Item = item;
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.Size = new Size(350, 84);
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9f);
        this.DoubleBuffered = true;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        Color accentColor = Item.Type switch
        {
            NotificationType.Success => ModernColors.Emerald500,
            NotificationType.Warning => ModernColors.Amber500,
            NotificationType.Error => ModernColors.Rose600,
            _ => ModernColors.Blue600
        };

        string iconText = Item.Type switch
        {
            NotificationType.Success => "✅",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "❌",
            _ => "ℹ️"
        };

        // Outer border & Accent stripe
        this.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);

            using var brushAccent = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(brushAccent, 0, 0, 5, this.Height);
        };

        var lblIcon = new Label
        {
            Text = iconText,
            Font = new Font("Segoe UI", 14f),
            Location = new Point(14, 14),
            Size = new Size(32, 32),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        lblIcon.Click += (_, _) => OnClickAction();

        var lblTitle = new Label
        {
            Text = Item.Title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(50, 10),
            Size = new Size(265, 20),
            Cursor = Cursors.Hand
        };
        lblTitle.Click += (_, _) => OnClickAction();

        var lblMsg = new Label
        {
            Text = Item.Message,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ModernColors.Slate600,
            Location = new Point(50, 32),
            Size = new Size(265, 34),
            Cursor = Cursors.Hand
        };
        lblMsg.Click += (_, _) => OnClickAction();

        var lblTime = new Label
        {
            Text = Item.TimeAgo,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(50, 66),
            AutoSize = true
        };

        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(22, 22),
            Location = new Point(this.Width - 26, 6),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => CloseToast();

        this.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblMsg, lblTime, btnClose });

        this.Click += (_, _) => OnClickAction();

        // Auto dismiss timer
        _dismissTimer = new System.Windows.Forms.Timer { Interval = 4500 };
        _dismissTimer.Tick += (_, _) => CloseToast();
        _dismissTimer.Start();
    }

    private void OnClickAction()
    {
        Item.IsRead = true;
        ActionClicked?.Invoke(Item);
        CloseToast();
    }

    public static void ShowToast(NotificationItem item, Action<NotificationItem>? onClick = null)
    {
        try
        {
            var toast = new ToastNotificationForm(item);
            if (onClick != null) toast.ActionClicked += onClick;

            // Position at bottom right, stacking above existing open toasts
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            int margin = 16;
            int x = workArea.Right - toast.Width - margin;
            int y = workArea.Bottom - toast.Height - margin;

            // Stack above existing
            foreach (var existing in _openToasts)
            {
                if (existing.Visible && !existing.IsDisposed)
                {
                    y -= (existing.Height + 10);
                }
            }

            if (y < workArea.Top) y = workArea.Bottom - toast.Height - margin;

            toast.Location = new Point(x, y);
            _openToasts.Add(toast);

            toast.FormClosed += (s, e) =>
            {
                _openToasts.Remove(toast);
                RepositionToasts();
            };

            toast.Show();
        }
        catch { }
    }

    private static void RepositionToasts()
    {
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int margin = 16;
        int y = workArea.Bottom - margin;

        for (int i = 0; i < _openToasts.Count; i++)
        {
            var t = _openToasts[i];
            if (t.Visible && !t.IsDisposed)
            {
                y -= t.Height;
                t.Location = new Point(workArea.Right - t.Width - margin, y);
                y -= 10;
            }
        }
    }

    private void CloseToast()
    {
        _dismissTimer?.Stop();
        this.Close();
    }
}
