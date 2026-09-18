using System;
using System.Management;

namespace SagarIsland;

public class BrightnessManager : IDisposable
{
    public event EventHandler<int>? BrightnessChanged;

    private ManagementEventWatcher? _watcher;
    private bool _isDisposed;

    public void Initialize()
    {
        try
        {
            var scope = new ManagementScope(@"root\wmi");
            var query = new WqlEventQuery("SELECT * FROM WmiMonitorBrightnessEvent");
            _watcher = new ManagementEventWatcher(scope, query);
            _watcher.EventArrived += Watcher_EventArrived;
            _watcher.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BrightnessManager] WMI Brightness watcher unavailable: {ex.Message}");
        }
    }

    private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            if (e.NewEvent.Properties["Brightness"]?.Value is byte b)
            {
                BrightnessChanged?.Invoke(this, b);
            }
            else if (int.TryParse(e.NewEvent.Properties["Brightness"]?.Value?.ToString(), out int brightness))
            {
                BrightnessChanged?.Invoke(this, brightness);
            }
        }
        catch { }
    }

    public int GetCurrentBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentBrightness"] is byte b) return b;
            }
        }
        catch { }
        return 65;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            try
            {
                _watcher?.Stop();
                _watcher?.Dispose();
            }
            catch { }
            _isDisposed = true;
        }
    }
}
