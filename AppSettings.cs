using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace SagarIsland;

public class AppSettings
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SagarIsland"
    );
    private static readonly string SettingsFilePath = Path.Combine(SettingsFolder, "settings.json");
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SagarIsland";

    // Appearance
    public double IslandWidth { get; set; } = 180.0;
    public double IslandHeight { get; set; } = 44.0;
    public double CornerRadius { get; set; } = 22.0;
    public double Opacity { get; set; } = 1.0;
    public string Theme { get; set; } = "Pitch Black";

    // Behavior
    public double AutoCollapseSeconds { get; set; } = 3.5;
    public int AnimationSpeedMs { get; set; } = 380;
    public bool ExpandOnHover { get; set; } = true;
    public bool ExpandOnClick { get; set; } = true;
    public bool AutoHideRestingIsland { get; set; } = true;
    public double RestingAutoHideSeconds { get; set; } = 4.0;

    // Features
    public bool EnableVolume { get; set; } = true;
    public bool EnableBrightness { get; set; } = true;
    public bool EnableBattery { get; set; } = true;
    public bool EnableMedia { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableClipboard { get; set; } = true;

    // System
    public bool IsEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    settings.StartWithWindows = IsStartupEnabled();
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
        }

        var defaults = new AppSettings();
        defaults.StartWithWindows = IsStartupEnabled();
        return defaults;
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);

            SetStartup(StartWithWindows);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Save error: {ex.Message}");
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "SagarIsland.exe");
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Registry startup error: {ex.Message}");
        }
    }
}
