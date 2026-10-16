using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Helpers;

namespace AsiasoftERP.UI.Forms.Modules;

public class NotificationDropdown : Form
{
    public event Action<string?>? ModuleRequested;

    private Panel _pnlList = null!;
    private Label _lblCountBadge = null!;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    public NotificationDropdown()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.Size = new Size(380, 480);
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9f);
        this.DoubleBuffered = true;

        InitializeComponents();
        RefreshList();

        this.Deactivate += (_, _) => { if (AutoCloseOnDeactivate) this.Close(); };
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool AutoCloseOnDeactivate { get; set; } = true;

    private void InitializeComponents()
    {
        this.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate700, 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        };

        // 1. Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = ModernColors.Slate900,
            Padding = new Padding(12, 0, 10, 0)
        };

        var lblTitle = new Label
        {
            Text = "🔔  Bildirimler",
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };

        _lblCountBadge = new Label
        {
            Text = "0 Yeni",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ModernColors.Rose600,
            Size = new Size(54, 22),
            Location = new Point(130, 11),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(26, 26),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => this.Close();

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, _lblCountBadge, btnClose });

        // 2. Action Sub-bar
        var pnlSubBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(10, 4, 10, 4)
        };
        pnlSubBar.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnlSubBar.Height - 1, pnlSubBar.Width, pnlSubBar.Height - 1);
        };

        var btnMarkRead = new Button
        {
            Text = "✓ Tümünü Okundu Say",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = ModernColors.Slate700,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Left,
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        btnMarkRead.FlatAppearance.BorderSize = 0;
        btnMarkRead.Click += (_, _) =>
        {
            NotificationService.MarkAllAsRead();
            RefreshList();
        };

        var btnClear = new Button
        {
            Text = "🗑️ Temizle",
            Font = new Font("Segoe UI", 8f),
            ForeColor = ModernColors.Rose600,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Dock = DockStyle.Right,
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.Click += (_, _) =>
        {
            NotificationService.ClearAll();
            RefreshList();
        };

        pnlSubBar.Controls.AddRange(new Control[] { btnMarkRead, btnClear });

        // 3. Scrollable List of Notifications
        _pnlList = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White
        };

        // 4. Footer
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = ModernColors.Slate100,
            Padding = new Padding(12, 6, 12, 6)
        };
        pnlFooter.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
        };

        var lblFooter = new Label
        {
            Text = "🟢 Tek Asya Canlı ERP Bildirim Servisi",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlFooter.Controls.Add(lblFooter);

        this.Controls.Add(_pnlList);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlSubBar);
        this.Controls.Add(pnlHeader);
    }

    public void RefreshList()
    {
        _pnlList.SuspendLayout();
        _pnlList.Controls.Clear();

        var notifications = NotificationService.GetNotifications();
        int unreadCount = NotificationService.GetUnreadCount();

        _lblCountBadge.Text = $"{unreadCount} Yeni";
        _lblCountBadge.Visible = unreadCount > 0;

        if (notifications.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Henüz yeni bir bildirim bulunmuyor.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlList.Controls.Add(lblEmpty);
            _pnlList.ResumeLayout();
            return;
        }

        int y = 0;
        foreach (var item in notifications)
        {
            var card = CreateNotificationCard(item, y);
            _pnlList.Controls.Add(card);
            y += card.Height + 1;
        }

        _pnlList.ResumeLayout();
    }

    private Panel CreateNotificationCard(NotificationItem item, int topY)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = item.IsRead ? Color.White : Color.FromArgb(248, 250, 252),
            Cursor = Cursors.Hand,
            Padding = new Padding(10, 8, 10, 8)
        };

        Color accentColor = item.Type switch
        {
            NotificationType.Success => ModernColors.Emerald500,
            NotificationType.Warning => ModernColors.Amber500,
            NotificationType.Error => ModernColors.Rose600,
            _ => ModernColors.Blue600
        };

        string iconText = item.Type switch
        {
            NotificationType.Success => "✅",
            NotificationType.Warning => "⚠️",
            NotificationType.Error => "❌",
            _ => "ℹ️"
        };

        pnl.Paint += (s, e) =>
        {
            using var pen = new Pen(ModernColors.Slate200, 1f);
            e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);

            // Left stripe
            using var brushAccent = new SolidBrush(accentColor);
            e.Graphics.FillRectangle(brushAccent, 0, 0, 4, pnl.Height);

            // Unread dot
            if (!item.IsRead)
            {
                using var dotBrush = new SolidBrush(ModernColors.Blue600);
                e.Graphics.FillEllipse(dotBrush, pnl.Width - 16, 12, 8, 8);
            }
        };

        var lblIcon = new Label
        {
            Text = iconText,
            Font = new Font("Segoe UI", 12f),
            Location = new Point(10, 10),
            Size = new Size(26, 26),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = item.Title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = ModernColors.Slate900,
            Location = new Point(42, 6),
            Size = new Size(290, 18)
        };

        var lblMsg = new Label
        {
            Text = item.Message,
            Font = new Font("Segoe UI", 8.2f),
            ForeColor = ModernColors.Slate600,
            Location = new Point(42, 25),
            Size = new Size(310, 28)
        };

        var lblTime = new Label
        {
            Text = item.TimeAgo,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(42, 53),
            AutoSize = true
        };

        pnl.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblMsg, lblTime });

        // Hover effect
        pnl.MouseEnter += (_, _) => pnl.BackColor = Color.FromArgb(241, 245, 249);
        pnl.MouseLeave += (_, _) => pnl.BackColor = item.IsRead ? Color.White : Color.FromArgb(248, 250, 252);

        void HandleClick()
        {
            item.IsRead = true;
            NotificationService.TriggerNotificationChanged();
            if (!string.IsNullOrEmpty(item.ModuleKey))
            {
                ModuleRequested?.Invoke(item.ModuleKey);
            }
            this.Close();
        }

        pnl.Click += (_, _) => HandleClick();
        lblIcon.Click += (_, _) => HandleClick();
        lblTitle.Click += (_, _) => HandleClick();
        lblMsg.Click += (_, _) => HandleClick();
        lblTime.Click += (_, _) => HandleClick();

        return pnl;
    }
}
