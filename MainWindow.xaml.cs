using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SagarIsland;

public enum IslandState
{
    Collapsed,
    ExpandedCard,
    VolumeHUD,
    MuteHUD,
    BrightnessHUD,
    BatteryHUD,
    MediaHUD,
    MediaExpanded,
    NotificationHUD,
    ClipboardHUD
}

/// <summary>
/// Dynamic Island Main Window & Fluid State Controller with Auto-Hide
/// </summary>
public partial class MainWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x08000000;
    private const uint SWP_SHOWWINDOW = 0x0040;

    // Default Dimensions
    private const double ExpandedWidth = 388.0;
    private const double ExpandedHeight = 172.0;

    private const double MediaExpandedWidth = 388.0;
    private const double MediaExpandedHeight = 200.0;

    private const double VolumeHUDWidth = 280.0;
    private const double VolumeHUDHeight = 60.0;

    private const double MuteHUDWidth = 200.0;
    private const double MuteHUDHeight = 44.0;

    private const double BrightnessHUDWidth = 280.0;
    private const double BrightnessHUDHeight = 60.0;

    private const double BatteryHUDWidth = 260.0;
    private const double BatteryHUDHeight = 56.0;

    private const double MediaHUDWidth = 348.0;
    private const double MediaHUDHeight = 60.0;

    private const double NotificationHUDWidth = 340.0;
    private const double NotificationHUDHeight = 60.0;

    private const double ClipboardHUDWidth = 288.0;
    private const double ClipboardHUDHeight = 56.0;

    // State & Timers
    private IslandState _currentState = IslandState.Collapsed;
    private DispatcherTimer? _activeWaitTimer;
    private DispatcherTimer? _clockTimer;
    private DispatcherTimer? _eqTimer;
    private DispatcherTimer? _restingAutoHideTimer;
    private readonly Random _random = new Random();

    private bool _isIslandHidden = false;
    private bool _hasActiveMedia = false;
    private bool _isMediaPlaying = false;
    private string _lastMediaTitle = "";
    private string _lastMediaArtist = "";
    private TimeSpan _currentMediaPos = TimeSpan.Zero;
    private TimeSpan _currentMediaDur = TimeSpan.Zero;

    // App Settings & Tray Icon
    private AppSettings _settings;
    private TrayIconManager? _trayManager;

    // Central Unified Event Manager
    private readonly IslandEventManager _eventManager;

    // Event Managers
    private VolumeManager? _volumeManager;
    private BrightnessManager? _brightnessManager;
    private BatteryManager? _batteryManager;
    private MediaManager? _mediaManager;
    private NotificationManager? _notificationManager;
    private ClipboardManager? _clipboardManager;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private static System.Windows.Media.SolidColorBrush HexBrush(string hex)
    {
        return (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;
    }

    public MainWindow()
    {
        InitializeComponent();

        // Enforce 120 FPS high refresh rate rendering for buttery smooth animations
        Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata(120));

        _settings = AppSettings.Load();
        _eventManager = new IslandEventManager(DispatchIslandEvent);

        _trayManager = new TrayIconManager(_settings, OnToggleEnabled, OpenSettings, OpenAbout);

        ApplySettings(_settings);

        _volumeManager = new VolumeManager();
        _volumeManager.VolumeChanged += VolumeManager_VolumeChanged;
        _volumeManager.Initialize();

        _brightnessManager = new BrightnessManager();
        _brightnessManager.BrightnessChanged += BrightnessManager_BrightnessChanged;
        _brightnessManager.Initialize();

        _batteryManager = new BatteryManager();
        _batteryManager.BatteryStatusChanged += BatteryManager_BatteryStatusChanged;
        _batteryManager.Initialize();

        _mediaManager = new MediaManager();
        _mediaManager.MediaChanged += MediaManager_MediaChanged;
        _mediaManager.Initialize();

        _notificationManager = new NotificationManager();
        _notificationManager.NotificationReceived += NotificationManager_NotificationReceived;
        _notificationManager.Initialize();

        _clipboardManager = new ClipboardManager();
        _clipboardManager.ClipboardChanged += ClipboardManager_ClipboardChanged;

        // Initialize Live Clock Timer (every 1 second)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
        UpdateClockDisplay();

        // Initialize Audio Equalizer Dance Timer (every 100ms)
        _eqTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _eqTimer.Tick += EqTimer_Tick;
        _eqTimer.Start();

        // Initialize Resting Auto-Hide Inactivity Timer
        _restingAutoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RestingAutoHideSeconds) };
        _restingAutoHideTimer.Tick += RestingAutoHideTimer_Tick;
        if (_settings.AutoHideRestingIsland)
        {
            _restingAutoHideTimer.Start();
        }

        Closing += (s, e) =>
        {
            _restingAutoHideTimer?.Stop();
            _clockTimer?.Stop();
            _eqTimer?.Stop();
            _trayManager?.Dispose();
            _volumeManager?.Dispose();
            _brightnessManager?.Dispose();
            _batteryManager?.Dispose();
            _mediaManager?.Dispose();
            _notificationManager?.Dispose();
            _clipboardManager?.Dispose();
        };
    }

    #region Settings & Tray Actions

    private void ApplySettings(AppSettings s)
    {
        _settings = s;

        if (_currentState == IslandState.Collapsed)
        {
            IslandPill.Width = _settings.IslandWidth;
            IslandPill.Height = _settings.IslandHeight;
            IslandPill.CornerRadius = new CornerRadius(_settings.CornerRadius);

            IslandShadowLayer.Width = _settings.IslandWidth;
            IslandShadowLayer.Height = _settings.IslandHeight;
            IslandShadowLayer.CornerRadius = new CornerRadius(_settings.CornerRadius);
        }

        IslandPill.Opacity = _settings.Opacity;
        IslandShadowLayer.Opacity = _settings.Opacity * 0.7;

        if (!_settings.IsEnabled)
        {
            CollapseToRestingPill();
            Visibility = Visibility.Collapsed;
        }
        else
        {
            Visibility = Visibility.Visible;
        }

        _trayManager?.UpdateStatus(_settings.IsEnabled, _settings.StartWithWindows);
    }

    private void OnToggleEnabled(bool isEnabled)
    {
        _settings.IsEnabled = isEnabled;
        ApplySettings(_settings);
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow(_settings, ApplySettings)
        {
            Owner = this
        };
        win.ShowDialog();
    }

    private void OpenAbout()
    {
        System.Windows.MessageBox.Show(
            "Dynamic Island for Windows\n\n" +
            "A fluid, native desktop island overlay with live volume, brightness, battery, media controls, and system notifications.\n\n" +
            "Developed by Sagar Murkute.",
            "About Dynamic Island",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    #endregion

    #region Window Setup & Win32 Topmost

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        IntPtr hwnd = helper.Handle;

        if (hwnd != IntPtr.Zero)
        {
            long exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

            _clipboardManager?.Initialize(this);
        }

        PositionWindowTopCenter();
    }

    private void PositionWindowTopCenter()
    {
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2;
        Top = 0;
    }

    #endregion

    #region Auto-Hide & Wake-Up System

    private void RestingAutoHideTimer_Tick(object? sender, EventArgs e)
    {
        _restingAutoHideTimer?.Stop();
        if (_currentState == IslandState.Collapsed)
        {
            HideRestingIsland();
        }
    }

    private void HideRestingIsland()
    {
        if (_isIslandHidden) return;
        _isIslandHidden = true;

        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var slideAnim = new DoubleAnimation
        {
            To = -52.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(320)),
            EasingFunction = ease
        };
        var fadeAnim = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(240)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        PillTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        ShadowTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);

        IslandPill.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        IslandShadowLayer.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
    }

    private void WakeUpRestingIsland(bool restartTimer = true)
    {
        _restingAutoHideTimer?.Stop();

        if (_isIslandHidden)
        {
            _isIslandHidden = false;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var slideAnim = new DoubleAnimation
            {
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(340)),
                EasingFunction = ease
            };
            var fadeAnim = new DoubleAnimation
            {
                To = _settings.Opacity,
                Duration = new Duration(TimeSpan.FromMilliseconds(240)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var shadowFadeAnim = new DoubleAnimation
            {
                To = _settings.Opacity * 0.50,
                Duration = new Duration(TimeSpan.FromMilliseconds(240)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            PillTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
            ShadowTranslate.BeginAnimation(TranslateTransform.YProperty, slideAnim);

            IslandPill.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            IslandShadowLayer.BeginAnimation(UIElement.OpacityProperty, shadowFadeAnim);
        }

        if (restartTimer && _settings.AutoHideRestingIsland && _currentState == IslandState.Collapsed)
        {
            _restingAutoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RestingAutoHideSeconds) };
            _restingAutoHideTimer.Tick += RestingAutoHideTimer_Tick;
            _restingAutoHideTimer.Start();
        }
    }

    private void TopEdgeHoverTrigger_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        WakeUpRestingIsland(restartTimer: false);
    }

    #endregion

    #region Live Clock & Audio Equalizer Animation

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClockDisplay();
    }

    private void UpdateClockDisplay()
    {
        var now = DateTime.Now;
        CollapsedTimeText.Text = now.ToString("h:mm tt");
        DashboardTimeText.Text = now.ToString("h:mm:ss tt");
        DashboardDateText.Text = now.ToString("dddd, MMMM d");
    }

    private void EqTimer_Tick(object? sender, EventArgs e)
    {
        if (_hasActiveMedia && _isMediaPlaying)
        {
            // Mini visualizer dance
            AnimateBar(MiniEQ1, 4, 14);
            AnimateBar(MiniEQ2, 3, 11);
            AnimateBar(MiniEQ3, 5, 15);
            AnimateBar(MiniEQ4, 3, 12);

            // Expanded visualizer dance
            AnimateBar(ExpEQ1, 4, 14);
            AnimateBar(ExpEQ2, 3, 11);
            AnimateBar(ExpEQ3, 5, 15);
            AnimateBar(ExpEQ4, 3, 12);
        }
    }

    private void AnimateBar(System.Windows.Controls.Border bar, double minH, double maxH)
    {
        if (bar == null) return;
        double targetH = minH + _random.NextDouble() * (maxH - minH);
        var anim = new DoubleAnimation
        {
            To = targetH,
            Duration = new Duration(TimeSpan.FromMilliseconds(80)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        bar.BeginAnimation(FrameworkElement.HeightProperty, anim);
    }

    #endregion

    #region Event Handlers & Priority Queue Dispatcher

    private void DispatchIslandEvent(IslandEvent evt)
    {
        Dispatcher.Invoke(() =>
        {
            WakeUpRestingIsland(restartTimer: false);
            evt.ApplyUI?.Invoke();
            PresentState(evt.State, evt.Width, evt.Height, GetViewForState(evt.State), evt.AutoCollapseSeconds);
        });
    }

    private UIElement GetViewForState(IslandState state) => state switch
    {
        IslandState.VolumeHUD => VolumeHUDView,
        IslandState.MuteHUD => MuteHUDView,
        IslandState.BrightnessHUD => BrightnessHUDView,
        IslandState.BatteryHUD => BatteryHUDView,
        IslandState.MediaHUD => MediaHUDView,
        IslandState.MediaExpanded => MediaExpandedView,
        IslandState.NotificationHUD => NotificationHUDView,
        IslandState.ClipboardHUD => ClipboardHUDView,
        IslandState.ExpandedCard => ExpandedView,
        _ => CollapsedView
    };

    private void VolumeManager_VolumeChanged(object? sender, VolumeChangedEventArgs e)
    {
        if (!_settings.IsEnabled || !_settings.EnableVolume) return;
        Dispatcher.Invoke(() =>
        {
            DashboardVolumeText.Text = $"{e.VolumePercent}%";
            DashboardVolumeProgress.Value = e.VolumePercent;

            if (e.IsMuted)
            {
                _eventManager.PostEvent(new IslandEvent(
                    IslandEventPriority.VolumeBrightnessBattery,
                    IslandState.MuteHUD,
                    MuteHUDWidth,
                    MuteHUDHeight,
                    _settings.AutoCollapseSeconds,
                    () => { }
                ), _currentState);
            }
            else
            {
                _eventManager.PostEvent(new IslandEvent(
                    IslandEventPriority.VolumeBrightnessBattery,
                    IslandState.VolumeHUD,
                    VolumeHUDWidth,
                    VolumeHUDHeight,
                    _settings.AutoCollapseSeconds,
                    () => UpdateVolumeUI(e.VolumePercent)
                ), _currentState);
            }
        });
    }

    private void BrightnessManager_BrightnessChanged(object? sender, int percent)
    {
        if (!_settings.IsEnabled || !_settings.EnableBrightness) return;
        Dispatcher.Invoke(() =>
        {
            _eventManager.PostEvent(new IslandEvent(
                IslandEventPriority.VolumeBrightnessBattery,
                IslandState.BrightnessHUD,
                BrightnessHUDWidth,
                BrightnessHUDHeight,
                _settings.AutoCollapseSeconds,
                () => UpdateBrightnessUI(percent)
            ), _currentState);
        });
    }

    private void BatteryManager_BatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        if (!_settings.IsEnabled || !_settings.EnableBattery) return;
        Dispatcher.Invoke(() =>
        {
            DashboardBatteryPercentText.Text = $"{e.Percent}%";
            DashboardBatteryProgress.Value = e.Percent;
            DashboardBatteryStateText.Text = e.PowerState == BatteryPowerState.Charging ? "Charging" : "Battery";

            _eventManager.PostEvent(new IslandEvent(
                IslandEventPriority.VolumeBrightnessBattery,
                IslandState.BatteryHUD,
                BatteryHUDWidth,
                BatteryHUDHeight,
                _settings.AutoCollapseSeconds + 0.5,
                () => UpdateBatteryUI(e)
            ), _currentState);
        });
    }

    private void NotificationManager_NotificationReceived(object? sender, NotificationItemEventArgs e)
    {
        if (!_settings.IsEnabled || !_settings.EnableNotifications) return;
        Dispatcher.Invoke(() =>
        {
            var priority = e.IsCritical ? IslandEventPriority.CriticalNotification : IslandEventPriority.NormalNotification;
            _eventManager.PostEvent(new IslandEvent(
                priority,
                IslandState.NotificationHUD,
                NotificationHUDWidth,
                NotificationHUDHeight,
                _settings.AutoCollapseSeconds + 0.5,
                () => UpdateNotificationUI(e)
            ), _currentState);
        });
    }

    private void ClipboardManager_ClipboardChanged(object? sender, ClipboardChangedEventArgs e)
    {
        if (!_settings.IsEnabled || !_settings.EnableClipboard) return;
        Dispatcher.Invoke(() =>
        {
            _eventManager.PostEvent(new IslandEvent(
                IslandEventPriority.Clipboard,
                IslandState.ClipboardHUD,
                ClipboardHUDWidth,
                ClipboardHUDHeight,
                _settings.AutoCollapseSeconds,
                () => UpdateClipboardUI(e)
            ), _currentState);
        });
    }

    private void UpdateNotificationUI(NotificationItemEventArgs e)
    {
        NotificationAppNameText.Text = e.AppName;
        NotificationTitleText.Text = e.Title;

        if (!string.IsNullOrWhiteSpace(e.Message))
        {
            NotificationMessageText.Text = e.Message;
            NotificationMessageText.Visibility = Visibility.Visible;
        }
        else
        {
            NotificationMessageText.Visibility = Visibility.Collapsed;
        }

        NotificationIconPath.Fill = e.IsCritical ? HexBrush("#FF453A") : HexBrush("#FF9F0A");
    }

    private void UpdateClipboardUI(ClipboardChangedEventArgs e)
    {
        ClipboardSummaryText.Text = e.Summary;
        ClipboardDetailText.Text = e.Detail;

        switch (e.ContentType)
        {
            case ClipboardContentType.Link:
                ClipboardBadgeIconPath.Data = (Geometry)FindResource("LinkIcon");
                ClipboardBadgeIconPath.Fill = HexBrush("#0A84FF");
                ClipboardBadgeBorder.Background = HexBrush("#142B42");
                ClipboardBadgeBorder.BorderBrush = HexBrush("#330A84FF");
                break;

            case ClipboardContentType.Image:
                ClipboardBadgeIconPath.Data = (Geometry)FindResource("ImageIcon");
                ClipboardBadgeIconPath.Fill = HexBrush("#BF5AF2");
                ClipboardBadgeBorder.Background = HexBrush("#351B42");
                ClipboardBadgeBorder.BorderBrush = HexBrush("#33BF5AF2");
                break;

            case ClipboardContentType.Files:
                ClipboardBadgeIconPath.Data = (Geometry)FindResource("FolderIcon");
                ClipboardBadgeIconPath.Fill = HexBrush("#FF9F0A");
                ClipboardBadgeBorder.Background = HexBrush("#3A2B14");
                ClipboardBadgeBorder.BorderBrush = HexBrush("#33FF9F0A");
                break;

            case ClipboardContentType.Text:
            default:
                ClipboardBadgeIconPath.Data = (Geometry)FindResource("CheckmarkIcon");
                ClipboardBadgeIconPath.Fill = HexBrush("#00E676");
                ClipboardBadgeBorder.Background = HexBrush("#1C3D27");
                ClipboardBadgeBorder.BorderBrush = HexBrush("#3300E676");
                break;
        }
    }

    private void UpdateBatteryUI(BatteryStatusEventArgs e)
    {
        BatteryPercentText.Text = $"{e.Percent}%";

        var barAnim = new DoubleAnimation
        {
            To = e.Percent,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BatteryProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, barAnim);

        switch (e.PowerState)
        {
            case BatteryPowerState.FullyCharged:
                BatteryStateTitleText.Text = "Fully Charged";
                BatteryStateTitleText.Foreground = HexBrush("#30D158");
                BatteryIconPath.Fill = HexBrush("#30D158");
                BatteryProgressBar.Foreground = HexBrush("#30D158");
                break;

            case BatteryPowerState.Charging:
                BatteryStateTitleText.Text = "Charging";
                BatteryStateTitleText.Foreground = HexBrush("#30D158");
                BatteryIconPath.Fill = HexBrush("#30D158");
                BatteryProgressBar.Foreground = HexBrush("#30D158");
                break;

            case BatteryPowerState.LowBattery:
                BatteryStateTitleText.Text = "Low Battery";
                BatteryStateTitleText.Foreground = HexBrush("#FF453A");
                BatteryIconPath.Fill = HexBrush("#FF453A");
                BatteryProgressBar.Foreground = HexBrush("#FF453A");
                break;

            case BatteryPowerState.NormalBattery:
            default:
                BatteryStateTitleText.Text = "Battery";
                BatteryStateTitleText.Foreground = HexBrush("#8E8E93");
                BatteryIconPath.Fill = HexBrush("#30D158");
                BatteryProgressBar.Foreground = HexBrush("#30D158");
                break;
        }
    }

    private void UpdateVolumeUI(int percent)
    {
        VolumePercentText.Text = $"{percent}%";

        var barAnim = new DoubleAnimation
        {
            To = percent,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        VolumeProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, barAnim);
    }

    private void UpdateBrightnessUI(int percent)
    {
        BrightnessPercentText.Text = $"{percent}%";

        var barAnim = new DoubleAnimation
        {
            To = percent,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BrightnessProgressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, barAnim);
    }

    private void MediaManager_MediaChanged(object? sender, MediaSessionChangedEventArgs e)
    {
        if (!_settings.IsEnabled || !_settings.EnableMedia) return;
        Dispatcher.Invoke(() =>
        {
            _hasActiveMedia = e.HasMedia;
            _isMediaPlaying = e.IsPlaying;
            _currentMediaPos = e.Position;
            _currentMediaDur = e.Duration;

            if (_hasActiveMedia && _isMediaPlaying)
            {
                WakeUpRestingIsland(restartTimer: false);
            }

            // Update Rest / Collapsed Ambient Glance
            if (_hasActiveMedia && _isMediaPlaying)
            {
                IdleDotIndicator.Visibility = Visibility.Collapsed;
                IdleMusicNote.Visibility = Visibility.Visible;
                CollapsedTimeText.Visibility = Visibility.Collapsed;
                CollapsedVisualizer.Visibility = Visibility.Visible;
                CollapsedCenterText.Text = e.Title;
            }
            else
            {
                IdleDotIndicator.Visibility = Visibility.Visible;
                IdleMusicNote.Visibility = Visibility.Collapsed;
                CollapsedTimeText.Visibility = Visibility.Visible;
                CollapsedVisualizer.Visibility = Visibility.Collapsed;
                CollapsedCenterText.Text = "Sagar Island";
            }

            if (!e.HasMedia)
            {
                _lastMediaTitle = "";
                _lastMediaArtist = "";
                if (_currentState == IslandState.MediaHUD || _currentState == IslandState.MediaExpanded)
                {
                    CollapseToRestingPill(_currentState == IslandState.MediaExpanded ? MediaExpandedView : MediaHUDView);
                }
                return;
            }

            bool isNewTrack = (_currentState != IslandState.MediaHUD && _currentState != IslandState.MediaExpanded) ||
                              (e.Title != _lastMediaTitle) ||
                              (e.Artist != _lastMediaArtist);

            _lastMediaTitle = e.Title;
            _lastMediaArtist = e.Artist;

            // Sync text to both compact and rich expanded views
            MediaTrackTitleText.Text = e.Title;
            MediaArtistText.Text = e.Artist;
            MediaExpandedTrackTitleText.Text = e.Title;
            MediaExpandedArtistText.Text = e.Artist;

            // Update timeline seek bar
            if (e.Duration.TotalSeconds > 0)
            {
                double pct = (e.Position.TotalSeconds / e.Duration.TotalSeconds) * 100.0;
                MediaTimelineProgressBar.Value = pct;
                MediaCurrentPosText.Text = $"{(int)e.Position.TotalMinutes:D2}:{e.Position.Seconds:D2}";
                MediaTotalDurText.Text = $"{(int)e.Duration.TotalMinutes:D2}:{e.Duration.Seconds:D2}";
            }
            else
            {
                MediaTimelineProgressBar.Value = 0;
                MediaCurrentPosText.Text = "00:00";
                MediaTotalDurText.Text = "--:--";
            }

            // Sync play/pause icons
            var playGeom = (Geometry)FindResource("PlayIcon");
            var pauseGeom = (Geometry)FindResource("PauseIcon");

            MediaActionIconPath.Data = e.IsPlaying ? pauseGeom : playGeom;
            MediaExpandedActionIconPath.Data = e.IsPlaying ? pauseGeom : playGeom;

            // Load Album Artwork if available
            if (e.ThumbnailBytes != null && e.ThumbnailBytes.Length > 0)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new System.IO.MemoryStream(e.ThumbnailBytes))
                    {
                        stream.Position = 0;
                        bitmap.BeginInit();
                        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();

                    MediaAlbumArtImage.Source = bitmap;
                    MediaAlbumArtImage.Visibility = Visibility.Visible;
                    MediaFallbackIconPath.Visibility = Visibility.Collapsed;

                    MediaExpandedAlbumArtImage.Source = bitmap;
                    MediaExpandedAlbumArtImage.Visibility = Visibility.Visible;
                    MediaExpandedFallbackIconPath.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    MediaAlbumArtImage.Visibility = Visibility.Collapsed;
                    MediaFallbackIconPath.Visibility = Visibility.Visible;
                    MediaExpandedAlbumArtImage.Visibility = Visibility.Collapsed;
                    MediaExpandedFallbackIconPath.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MediaAlbumArtImage.Visibility = Visibility.Collapsed;
                MediaFallbackIconPath.Visibility = Visibility.Visible;
                MediaExpandedAlbumArtImage.Visibility = Visibility.Collapsed;
                MediaExpandedFallbackIconPath.Visibility = Visibility.Visible;
            }

            // If already in MediaExpanded More Info view:
            if (_currentState == IslandState.MediaExpanded)
            {
                AnimateExpandedButtonPulse();
                ResetAutoCollapseTimer(MediaExpandedView, 10.0);
                return;
            }

            // If Island is already expanded in MediaHUD and track is the same (e.g. play/pause toggled)
            if (!isNewTrack && _currentState == IslandState.MediaHUD)
            {
                AnimateButtonPulse();
                ResetAutoCollapseTimer(MediaHUDView, 3.5);
                return;
            }

            // Route New Track Event through Priority Manager
            _eventManager.PostEvent(new IslandEvent(
                IslandEventPriority.Media,
                IslandState.MediaHUD,
                MediaHUDWidth,
                MediaHUDHeight,
                4.0,
                () => AnimateNewTrackEntrance()
            ), _currentState);
        });
    }

    private void PresentMediaExpanded()
    {
        _eventManager.ClearCurrent();
        PresentState(IslandState.MediaExpanded, MediaExpandedWidth, MediaExpandedHeight, MediaExpandedView, autoCollapseSeconds: 10.0);
    }

    private void AnimateNewTrackEntrance()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var quadEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        // 1. Artwork / Content slides in from left
        MediaArtworkContainer.Opacity = 0.0;
        MediaArtworkTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
        {
            From = -20.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(260)),
            EasingFunction = ease
        });
        MediaArtworkContainer.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            EasingFunction = quadEase
        });

        // 2. Track information appears (staggered slightly)
        MediaInfoPanel.Opacity = 0.0;
        MediaInfoTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
        {
            From = 16.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(260)),
            BeginTime = TimeSpan.FromMilliseconds(40),
            EasingFunction = ease
        });
        MediaInfoPanel.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            BeginTime = TimeSpan.FromMilliseconds(40),
            EasingFunction = quadEase
        });

        // 3. Media Controls appear
        MediaControlsPanel.Opacity = 0.0;
        MediaControlsPanel.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            BeginTime = TimeSpan.FromMilliseconds(80),
            EasingFunction = quadEase
        });
    }

    private void AnimateButtonPulse()
    {
        var scaleAnim = new DoubleAnimation
        {
            From = 1.15,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        MediaActionScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        MediaActionScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    private void AnimateExpandedButtonPulse()
    {
        var scaleAnim = new DoubleAnimation
        {
            From = 1.15,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(180)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        MediaExpandedActionScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        MediaExpandedActionScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    private void ResetAutoCollapseTimer(UIElement activeView, double seconds)
    {
        _activeWaitTimer?.Stop();
        if (seconds > 0)
        {
            _activeWaitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _activeWaitTimer.Tick += (s, ev) =>
            {
                _activeWaitTimer?.Stop();
                CollapseToRestingPill(activeView);
            };
            _activeWaitTimer.Start();
        }
    }

    private async void MediaPrevButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        double sec = (_currentState == IslandState.MediaExpanded) ? 10.0 : 3.5;
        UIElement view = (_currentState == IslandState.MediaExpanded) ? MediaExpandedView : MediaHUDView;
        ResetAutoCollapseTimer(view, sec);
        if (_mediaManager != null)
        {
            await _mediaManager.PrevTrackAsync();
        }
    }

    private async void MediaActionButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        AnimateButtonPulse();
        ResetAutoCollapseTimer(MediaHUDView, 3.5);
        if (_mediaManager != null)
        {
            await _mediaManager.TogglePlayPauseAsync();
        }
    }

    private async void MediaExpandedActionButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        AnimateExpandedButtonPulse();
        ResetAutoCollapseTimer(MediaExpandedView, 10.0);
        if (_mediaManager != null)
        {
            await _mediaManager.TogglePlayPauseAsync();
        }
    }

    private async void MediaNextButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        double sec = (_currentState == IslandState.MediaExpanded) ? 10.0 : 3.5;
        UIElement view = (_currentState == IslandState.MediaExpanded) ? MediaExpandedView : MediaHUDView;
        ResetAutoCollapseTimer(view, sec);
        if (_mediaManager != null)
        {
            await _mediaManager.NextTrackAsync();
        }
    }

    private async void MediaReplay10Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ResetAutoCollapseTimer(MediaExpandedView, 10.0);
        if (_mediaManager != null)
        {
            await _mediaManager.SkipSecondsAsync(-10.0);
        }
    }

    private async void MediaForward10Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ResetAutoCollapseTimer(MediaExpandedView, 10.0);
        if (_mediaManager != null)
        {
            await _mediaManager.SkipSecondsAsync(10.0);
        }
    }

    private async void TimelineProgress_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ResetAutoCollapseTimer(MediaExpandedView, 10.0);
        if (_mediaManager != null && sender is FrameworkElement elem)
        {
            var pt = e.GetPosition(elem);
            double pct = pt.X / elem.ActualWidth;
            await _mediaManager.SeekToPercentAsync(pct);
        }
    }

    private void SettingsButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        CollapseToRestingPill();
        OpenSettings();
    }

    private void PresentState(IslandState newState, double targetWidth, double targetHeight, UIElement activeView, double autoCollapseSeconds)
    {
        WakeUpRestingIsland(restartTimer: false);

        // Don't interrupt user-opened full expanded cards with HUD notifications unless state is Collapsed
        if ((_currentState == IslandState.ExpandedCard || _currentState == IslandState.MediaExpanded) &&
            newState != IslandState.Collapsed && newState != IslandState.MediaExpanded && newState != IslandState.ExpandedCard)
        {
            return;
        }

        _activeWaitTimer?.Stop();

        // If already in this state, update in-place without restarting expansion animation
        if (_currentState == newState && activeView.Visibility == Visibility.Visible && activeView.Opacity >= 0.8)
        {
            if (autoCollapseSeconds > 0)
            {
                _activeWaitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(autoCollapseSeconds) };
                _activeWaitTimer.Tick += (s, ev) =>
                {
                    _activeWaitTimer?.Stop();
                    CollapseToRestingPill(activeView);
                };
                _activeWaitTimer.Start();
            }
            return;
        }

        _currentState = newState;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animDur = new Duration(TimeSpan.FromMilliseconds(Math.Min(_settings.AnimationSpeedMs, 320)));

        double targetRadius = (newState == IslandState.ExpandedCard || newState == IslandState.MediaExpanded) ? 24.0
            : (newState == IslandState.MuteHUD || newState == IslandState.Collapsed) ? 22.0
            : 20.0;
        IslandPill.CornerRadius = new CornerRadius(targetRadius);
        IslandShadowLayer.CornerRadius = new CornerRadius(targetRadius);

        // 1. Fluid Synchronized Animation for Pill and Decoupled Shadow
        IslandPill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = targetWidth, Duration = animDur, EasingFunction = ease });
        IslandPill.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = targetHeight, Duration = animDur, EasingFunction = ease });

        IslandShadowLayer.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = targetWidth, Duration = animDur, EasingFunction = ease });
        IslandShadowLayer.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = targetHeight, Duration = animDur, EasingFunction = ease });

        // 2. Hide other views
        CollapsedView.Visibility = Visibility.Collapsed;
        ExpandedView.Visibility = (activeView == ExpandedView) ? Visibility.Visible : Visibility.Collapsed;
        VolumeHUDView.Visibility = (activeView == VolumeHUDView) ? Visibility.Visible : Visibility.Collapsed;
        MuteHUDView.Visibility = (activeView == MuteHUDView) ? Visibility.Visible : Visibility.Collapsed;
        BrightnessHUDView.Visibility = (activeView == BrightnessHUDView) ? Visibility.Visible : Visibility.Collapsed;
        BatteryHUDView.Visibility = (activeView == BatteryHUDView) ? Visibility.Visible : Visibility.Collapsed;
        MediaHUDView.Visibility = (activeView == MediaHUDView) ? Visibility.Visible : Visibility.Collapsed;
        MediaExpandedView.Visibility = (activeView == MediaExpandedView) ? Visibility.Visible : Visibility.Collapsed;
        NotificationHUDView.Visibility = (activeView == NotificationHUDView) ? Visibility.Visible : Visibility.Collapsed;
        ClipboardHUDView.Visibility = (activeView == ClipboardHUDView) ? Visibility.Visible : Visibility.Collapsed;

        // 3. Fade in Active View
        activeView.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
        {
            From = activeView.Opacity,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });

        // 4. Inactivity Wait Timer -> Collapse
        if (autoCollapseSeconds > 0)
        {
            _activeWaitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(autoCollapseSeconds) };
            _activeWaitTimer.Tick += (s, ev) =>
            {
                _activeWaitTimer?.Stop();
                CollapseToRestingPill(activeView);
            };
            _activeWaitTimer.Start();
        }
    }

    private void CollapseToRestingPill(UIElement? activeView = null)
    {
        _activeWaitTimer?.Stop();

        var fadeOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(100)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (s, ev) =>
        {
            var nextEvt = _eventManager.GetNextQueuedEvent();
            if (nextEvt != null)
            {
                DispatchIslandEvent(nextEvt);
                return;
            }

            _eventManager.ClearCurrent();

            // Hide all subviews
            ExpandedView.Visibility = Visibility.Collapsed;
            VolumeHUDView.Visibility = Visibility.Collapsed;
            MuteHUDView.Visibility = Visibility.Collapsed;
            BrightnessHUDView.Visibility = Visibility.Collapsed;
            BatteryHUDView.Visibility = Visibility.Collapsed;
            MediaHUDView.Visibility = Visibility.Collapsed;
            MediaExpandedView.Visibility = Visibility.Collapsed;
            NotificationHUDView.Visibility = Visibility.Collapsed;
            ClipboardHUDView.Visibility = Visibility.Collapsed;
            _currentState = IslandState.Collapsed;

            IslandPill.CornerRadius = new CornerRadius(22);
            IslandShadowLayer.CornerRadius = new CornerRadius(22);

            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
            var animDur = new Duration(TimeSpan.FromMilliseconds(240));

            // Morph pill and shadow back to resting dimensions
            IslandPill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = _settings.IslandWidth, Duration = animDur, EasingFunction = ease });
            IslandPill.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = _settings.IslandHeight, Duration = animDur, EasingFunction = ease });

            IslandShadowLayer.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = _settings.IslandWidth, Duration = animDur, EasingFunction = ease });
            IslandShadowLayer.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = _settings.IslandHeight, Duration = animDur, EasingFunction = ease });

            // Restore collapsed pill glance
            CollapsedView.Visibility = Visibility.Visible;
            CollapsedView.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                BeginTime = TimeSpan.FromMilliseconds(60)
            });

            // Start auto-hide inactivity timer if resting island is configured to hide
            if (_settings.AutoHideRestingIsland)
            {
                _restingAutoHideTimer?.Stop();
                _restingAutoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RestingAutoHideSeconds) };
                _restingAutoHideTimer.Tick += RestingAutoHideTimer_Tick;
                _restingAutoHideTimer.Start();
            }
        };

        if (activeView != null && activeView.Visibility == Visibility.Visible)
        {
            activeView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else
        {
            ExpandedView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            VolumeHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            MuteHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            BrightnessHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            BatteryHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            MediaHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            MediaExpandedView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            NotificationHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            ClipboardHUDView.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }

    #endregion

    #region User Mouse Interactions (Click & Hover)

    private void IslandPill_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        WakeUpRestingIsland(restartTimer: false);

        if (_currentState == IslandState.Collapsed)
        {
            if (!_settings.ExpandOnClick) return;

            if (_hasActiveMedia)
            {
                // Touch when media is active -> Extend to More Info (10s)
                PresentMediaExpanded();
            }
            else
            {
                // Click collapsed -> Expand Smart Quick Dashboard (4s)
                _eventManager.ClearCurrent();
                PresentState(IslandState.ExpandedCard, ExpandedWidth, ExpandedHeight, ExpandedView, autoCollapseSeconds: 4);
            }
        }
        else if (_currentState == IslandState.MediaHUD)
        {
            // Touch compact media HUD banner -> Extend to rich More Info card (10s)
            PresentMediaExpanded();
        }
        else
        {
            // Click when already in ExpandedCard or MediaExpanded -> Immediately Collapse
            _eventManager.ClearCurrent();
            CollapseToRestingPill();
        }
    }

    private void IslandPill_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        WakeUpRestingIsland(restartTimer: false);

        if (_currentState == IslandState.Collapsed)
        {
            if (!_settings.ExpandOnHover) return;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var dur = new Duration(TimeSpan.FromMilliseconds(160));

            double hoverW = _settings.IslandWidth + 12.0;
            double hoverH = _settings.IslandHeight + 2.0;

            IslandPill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = hoverW, Duration = dur, EasingFunction = ease });
            IslandPill.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = hoverH, Duration = dur, EasingFunction = ease });

            IslandShadowLayer.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = hoverW, Duration = dur, EasingFunction = ease });
            IslandShadowLayer.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = hoverH, Duration = dur, EasingFunction = ease });
        }
        else
        {
            // Pause auto-collapse countdown while user is hovering
            _activeWaitTimer?.Stop();
        }
    }

    private void IslandPill_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_currentState == IslandState.Collapsed)
        {
            if (_settings.ExpandOnHover)
            {
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var dur = new Duration(TimeSpan.FromMilliseconds(180));

                IslandPill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = _settings.IslandWidth, Duration = dur, EasingFunction = ease });
                IslandPill.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = _settings.IslandHeight, Duration = dur, EasingFunction = ease });

                IslandShadowLayer.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation { To = _settings.IslandWidth, Duration = dur, EasingFunction = ease });
                IslandShadowLayer.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation { To = _settings.IslandHeight, Duration = dur, EasingFunction = ease });
            }

            // Start auto-hide inactivity timer on mouse leave
            if (_settings.AutoHideRestingIsland)
            {
                _restingAutoHideTimer?.Stop();
                _restingAutoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RestingAutoHideSeconds) };
                _restingAutoHideTimer.Tick += RestingAutoHideTimer_Tick;
                _restingAutoHideTimer.Start();
            }
        }
        else
        {
            // Resume auto-collapse countdown on mouse leave
            double remainSec = (_currentState == IslandState.MediaExpanded) ? 10.0 : _settings.AutoCollapseSeconds;
            UIElement activeView = GetViewForState(_currentState);
            ResetAutoCollapseTimer(activeView, remainSec);
        }
    }

    #endregion
}