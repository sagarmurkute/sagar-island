using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace SagarIsland;

public class NotificationItemEventArgs : EventArgs
{
    public string AppName { get; }
    public string Title { get; }
    public string Message { get; }
    public bool IsCritical { get; }

    public NotificationItemEventArgs(string appName, string title, string message, bool isCritical = false)
    {
        AppName = string.IsNullOrWhiteSpace(appName) ? "Notification" : appName;
        Title = string.IsNullOrWhiteSpace(title) ? "New Alert" : title;
        Message = string.IsNullOrWhiteSpace(message) ? "" : message;
        IsCritical = isCritical;
    }
}

public class NotificationManager : IDisposable
{
    public event EventHandler<NotificationItemEventArgs>? NotificationReceived;

    private UserNotificationListener? _listener;
    private uint _lastNotificationId = 0;
    private DateTime _lastNotificationTime = DateTime.MinValue;
    private bool _isDisposed = false;

    public async void Initialize()
    {
        try
        {
            if (UserNotificationListener.Current != null)
            {
                _listener = UserNotificationListener.Current;
                var access = await _listener.RequestAccessAsync();
                if (access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _listener.NotificationChanged += Listener_NotificationChanged;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationManager] Init error: {ex.Message}");
        }
    }

    private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added)
        {
            return;
        }

        try
        {
            var notif = sender.GetNotification(args.UserNotificationId);
            if (notif == null) return;

            if (notif.Id == _lastNotificationId && (DateTime.UtcNow - _lastNotificationTime).TotalMilliseconds < 1500)
            {
                return;
            }

            _lastNotificationId = notif.Id;
            _lastNotificationTime = DateTime.UtcNow;

            string appName = notif.AppInfo?.DisplayInfo?.DisplayName ?? "Windows";
            string title = "";
            string message = "";

            var binding = notif.Notification?.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding != null)
            {
                var textElements = binding.GetTextElements().ToList();
                if (textElements.Count > 0)
                {
                    title = textElements[0].Text ?? "";
                }
                if (textElements.Count > 1)
                {
                    message = string.Join(" ", textElements.Skip(1).Select(t => t.Text));
                }
            }

            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(message))
            {
                bool isCritical = title.Contains("Emergency", StringComparison.OrdinalIgnoreCase) ||
                                  title.Contains("Critical", StringComparison.OrdinalIgnoreCase) ||
                                  title.Contains("Alert", StringComparison.OrdinalIgnoreCase);

                NotificationReceived?.Invoke(this, new NotificationItemEventArgs(appName, title, message, isCritical));
            }
        }
        catch
        {
            // Transient notification read failure
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_listener != null)
            {
                _listener.NotificationChanged -= Listener_NotificationChanged;
            }
            _isDisposed = true;
        }
    }
}
