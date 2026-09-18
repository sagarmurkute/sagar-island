using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SagarIsland;

public enum BatteryPowerState
{
    NormalBattery,
    Charging,
    FullyCharged,
    LowBattery
}

public class BatteryStatusEventArgs : EventArgs
{
    public int Percent { get; }
    public bool IsCharging { get; }
    public bool IsPluggedIn { get; }
    public bool HasBattery { get; }
    public BatteryPowerState PowerState { get; }

    public BatteryStatusEventArgs(int percent, bool isCharging, bool isPluggedIn, bool hasBattery)
    {
        Percent = Math.Clamp(percent, 0, 100);
        IsCharging = isCharging;
        IsPluggedIn = isPluggedIn;
        HasBattery = hasBattery;

        if (isPluggedIn && percent >= 99)
        {
            PowerState = BatteryPowerState.FullyCharged;
        }
        else if (isPluggedIn || isCharging)
        {
            PowerState = BatteryPowerState.Charging;
        }
        else if (percent <= 20)
        {
            PowerState = BatteryPowerState.LowBattery;
        }
        else
        {
            PowerState = BatteryPowerState.NormalBattery;
        }
    }
}

public class BatteryManager : IDisposable
{
    public event EventHandler<BatteryStatusEventArgs>? BatteryStatusChanged;

    private DispatcherTimer? _pollTimer;
    private bool _lastPluggedIn = false;
    private int _lastPercent = -1;
    private int _lastLowAlertThreshold = 100;
    private bool _isDisposed = false;

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus; // 0: Offline, 1: Online, 255: Unknown
        public byte BatteryFlag;  // 1: High, 2: Low, 4: Critical, 8: Charging, 128: No Battery, 255: Unknown
        public byte BatteryLifePercent; // 0-100, 255: Unknown
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    public void Initialize()
    {
        // Read initial state
        var initial = QueryCurrentStatus();
        _lastPluggedIn = initial.IsPluggedIn;
        _lastPercent = initial.Percent;

        // Listen to Windows PowerMode changes (plugged/unplugged/sleep)
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        // Periodic check to capture live percentage changes & charging transitions
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (s, e) => CheckStatus();
        _pollTimer.Start();
    }

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        CheckStatus(forceNotify: true);
    }

    public BatteryStatusEventArgs QueryCurrentStatus()
    {
        if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
        {
            bool hasBattery = status.BatteryFlag != 128 && status.BatteryLifePercent != 255;
            int percent = hasBattery ? (int)status.BatteryLifePercent : 100;
            bool isPluggedIn = status.ACLineStatus == 1;
            bool isCharging = (status.BatteryFlag & 8) != 0 || isPluggedIn;

            return new BatteryStatusEventArgs(percent, isCharging, isPluggedIn, hasBattery);
        }

        return new BatteryStatusEventArgs(100, true, true, false);
    }

    private void CheckStatus(bool forceNotify = false)
    {
        var current = QueryCurrentStatus();

        if (!current.HasBattery)
        {
            // Desktop machine without battery: do not trigger false notifications
            return;
        }

        bool pluggedChanged = current.IsPluggedIn != _lastPluggedIn;
        bool percentChanged = Math.Abs(current.Percent - _lastPercent) >= 1;

        if (current.IsPluggedIn)
        {
            // Reset low battery threshold when user is plugged into power
            _lastLowAlertThreshold = 100;
        }

        // Check if crossed a new low battery threshold (20%, 10%, 5%)
        bool lowBatteryMilestone = false;
        if (!current.IsPluggedIn)
        {
            if (current.Percent <= 5 && _lastLowAlertThreshold > 5)
            {
                lowBatteryMilestone = true;
                _lastLowAlertThreshold = 5;
            }
            else if (current.Percent <= 10 && _lastLowAlertThreshold > 10)
            {
                lowBatteryMilestone = true;
                _lastLowAlertThreshold = 10;
            }
            else if (current.Percent <= 20 && _lastLowAlertThreshold > 20)
            {
                lowBatteryMilestone = true;
                _lastLowAlertThreshold = 20;
            }
        }

        if (forceNotify || pluggedChanged || lowBatteryMilestone)
        {
            _lastPluggedIn = current.IsPluggedIn;
            _lastPercent = current.Percent;
            BatteryStatusChanged?.Invoke(this, current);
        }
        else if (percentChanged)
        {
            _lastPercent = current.Percent;
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _pollTimer?.Stop();
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            _isDisposed = true;
        }
    }
}
