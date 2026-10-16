using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AsiasoftERP.Core.Models;
using AsiasoftERP.UI.Forms.Modules;

namespace AsiasoftERP.UI.Helpers;

public static class NotificationService
{
    private static readonly List<NotificationItem> _notifications = new();
    private static readonly object _lock = new();

    public static event Action? NotificationsChanged;
    public static event Action<string?>? ModuleRequested;

    static NotificationService()
    {
        // Seed initial notifications for realistic live factory operation
        _notifications.Add(new NotificationItem
        {
            Title = "Yükleme Programı Onaylandı",
            Message = "Kuveyt & Katar 40HQ konteyner yükleme planı onaylandı.",
            Timestamp = DateTime.Now.AddMinutes(-12),
            Type = NotificationType.Success,
            IsRead = false,
            ModuleKey = "YuklemeProgrami"
        });

        _notifications.Add(new NotificationItem
        {
            Title = "Kantar Tartım Tamamlandı",
            Message = "33 ABN 421 (Limon Kasaları) 24.850 KG tartım fişi kesildi.",
            Timestamp = DateTime.Now.AddMinutes(-34),
            Type = NotificationType.Info,
            IsRead = false,
            ModuleKey = "MalKabul"
        });

        _notifications.Add(new NotificationItem
        {
            Title = "Etilen & Sarartma Odası #3",
            Message = "Oda #3 sıcaklık 18.2°C, gaz seviyesi optimum aralıkta.",
            Timestamp = DateTime.Now.AddHours(-1),
            Type = NotificationType.Info,
            IsRead = true,
            ModuleKey = "Etilen"
        });

        _notifications.Add(new NotificationItem
        {
            Title = "Ödeme Talebi Bekliyor",
            Message = "PETKİM hammadde tedariği için 125.000 TL onay bekliyor.",
            Timestamp = DateTime.Now.AddHours(-2),
            Type = NotificationType.Warning,
            IsRead = true,
            ModuleKey = "OdemeTalep"
        });
    }

    public static IReadOnlyList<NotificationItem> GetNotifications()
    {
        lock (_lock)
        {
            return _notifications.OrderByDescending(n => n.Timestamp).ToList();
        }
    }

    public static int GetUnreadCount()
    {
        lock (_lock)
        {
            return _notifications.Count(n => !n.IsRead);
        }
    }

    public static void Notify(string title, string message, NotificationType type = NotificationType.Info, string? moduleKey = null, bool showToast = true)
    {
        var item = new NotificationItem
        {
            Title = title,
            Message = message,
            Type = type,
            Timestamp = DateTime.Now,
            IsRead = false,
            ModuleKey = moduleKey
        };

        lock (_lock)
        {
            _notifications.Insert(0, item);
            if (_notifications.Count > 100)
            {
                _notifications.RemoveAt(_notifications.Count - 1);
            }
        }

        TriggerNotificationChanged();

        if (showToast)
        {
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form mainForm && !mainForm.IsDisposed)
            {
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => ShowToastInternal(item)));
                }
                else
                {
                    ShowToastInternal(item);
                }
            }
            else
            {
                ShowToastInternal(item);
            }
        }
    }

    private static void ShowToastInternal(NotificationItem item)
    {
        ToastNotificationForm.ShowToast(item, clickedItem =>
        {
            clickedItem.IsRead = true;
            TriggerNotificationChanged();
            if (!string.IsNullOrEmpty(clickedItem.ModuleKey))
            {
                ModuleRequested?.Invoke(clickedItem.ModuleKey);
            }
        });
    }

    public static void NotifySuccess(string title, string message, string? moduleKey = null, bool showToast = true)
        => Notify(title, message, NotificationType.Success, moduleKey, showToast);

    public static void NotifyWarning(string title, string message, string? moduleKey = null, bool showToast = true)
        => Notify(title, message, NotificationType.Warning, moduleKey, showToast);

    public static void NotifyError(string title, string message, string? moduleKey = null, bool showToast = true)
        => Notify(title, message, NotificationType.Error, moduleKey, showToast);

    public static void NotifyInfo(string title, string message, string? moduleKey = null, bool showToast = true)
        => Notify(title, message, NotificationType.Info, moduleKey, showToast);

    public static void MarkAllAsRead()
    {
        lock (_lock)
        {
            foreach (var n in _notifications)
            {
                n.IsRead = true;
            }
        }
        TriggerNotificationChanged();
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
        TriggerNotificationChanged();
    }

    public static void TriggerNotificationChanged()
    {
        try
        {
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0] is Form form && !form.IsDisposed)
            {
                if (form.InvokeRequired)
                {
                    form.BeginInvoke(new Action(() => NotificationsChanged?.Invoke()));
                    return;
                }
            }
            NotificationsChanged?.Invoke();
        }
        catch { }
    }
}
