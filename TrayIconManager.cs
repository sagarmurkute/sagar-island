using System;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SagarIsland;

public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _enabledMenuItem;
    private ToolStripMenuItem? _startupMenuItem;

    private readonly AppSettings _settings;
    private readonly Action<bool> _onToggleEnabled;
    private readonly Action _onOpenSettings;
    private readonly Action _onOpenAbout;
    private bool _isDisposed = false;

    public TrayIconManager(
        AppSettings settings,
        Action<bool> onToggleEnabled,
        Action onOpenSettings,
        Action onOpenAbout)
    {
        _settings = settings;
        _onToggleEnabled = onToggleEnabled;
        _onOpenSettings = onOpenSettings;
        _onOpenAbout = onOpenAbout;

        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _contextMenu = new ContextMenuStrip();

        // 1. Header (bold disabled item)
        var titleItem = new ToolStripMenuItem("Dynamic Island")
        {
            Enabled = false,
            Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold)
        };
        _contextMenu.Items.Add(titleItem);
        _contextMenu.Items.Add(new ToolStripSeparator());

        // 2. Enabled Toggle
        _enabledMenuItem = new ToolStripMenuItem("Enabled")
        {
            Checked = _settings.IsEnabled,
            CheckOnClick = true
        };
        _enabledMenuItem.Click += (s, e) =>
        {
            _settings.IsEnabled = _enabledMenuItem.Checked;
            _settings.Save();
            _onToggleEnabled(_settings.IsEnabled);
        };
        _contextMenu.Items.Add(_enabledMenuItem);

        // 3. Settings Item
        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (s, e) => _onOpenSettings();
        _contextMenu.Items.Add(settingsItem);

        // 4. Start with Windows Toggle
        _startupMenuItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = _settings.StartWithWindows,
            CheckOnClick = true
        };
        _startupMenuItem.Click += (s, e) =>
        {
            _settings.StartWithWindows = _startupMenuItem.Checked;
            _settings.Save();
        };
        _contextMenu.Items.Add(_startupMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 5. About Item
        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (s, e) => _onOpenAbout();
        _contextMenu.Items.Add(aboutItem);

        // 6. Exit Item
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) =>
        {
            Dispose();
            Application.Current.Shutdown();
        };
        _contextMenu.Items.Add(exitItem);

        // Create Icon (Draw crisp high-contrast pill icon)
        Icon trayIcon = CreatePillIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "Dynamic Island for Windows",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => _onOpenSettings();
    }

    public void UpdateStatus(bool isEnabled, bool startWithWindows)
    {
        if (_enabledMenuItem != null)
        {
            _enabledMenuItem.Checked = isEnabled;
        }
        if (_startupMenuItem != null)
        {
            _startupMenuItem.Checked = startWithWindows;
        }
    }

    private Icon CreatePillIcon()
    {
        // Generate a clean 32x32 Dynamic Island icon dynamically
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Black Pill Background
            using var brush = new SolidBrush(Color.FromArgb(240, 16, 16, 16));
            using var borderPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f);
            using var dotBrush = new SolidBrush(Color.FromArgb(255, 0, 230, 118));

            // Rounded Pill Capsule
            var rect = new Rectangle(2, 9, 28, 14);
            using var path = GetRoundedRect(rect, 7);
            g.FillPath(brush, path);
            g.DrawPath(borderPen, path);

            // Green Live Dot indicator inside pill
            g.FillEllipse(dotBrush, 7, 13, 6, 6);
        }

        IntPtr hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _contextMenu?.Dispose();
            _isDisposed = true;
        }
    }
}
