using System;
using System.Windows;

namespace SagarIsland;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings> _onSettingsSaved;

    public SettingsWindow(AppSettings settings, Action<AppSettings> onSettingsSaved)
    {
        InitializeComponent();
        _settings = settings;
        _onSettingsSaved = onSettingsSaved;

        LoadValues();
    }

    private void LoadValues()
    {
        // Features
        ChkVolume.IsChecked = _settings.EnableVolume;
        ChkBrightness.IsChecked = _settings.EnableBrightness;
        ChkBattery.IsChecked = _settings.EnableBattery;
        ChkMedia.IsChecked = _settings.EnableMedia;
        ChkNotifications.IsChecked = _settings.EnableNotifications;
        ChkClipboard.IsChecked = _settings.EnableClipboard;

        // Behavior
        ChkExpandOnHover.IsChecked = _settings.ExpandOnHover;
        ChkExpandOnClick.IsChecked = _settings.ExpandOnClick;
        SliderAutoCollapse.Value = _settings.AutoCollapseSeconds;
        SliderAnimSpeed.Value = _settings.AnimationSpeedMs;

        // Appearance
        SliderWidth.Value = _settings.IslandWidth;
        SliderHeight.Value = _settings.IslandHeight;
        SliderOpacity.Value = _settings.Opacity;

        // Startup
        ChkStartup.IsChecked = _settings.StartWithWindows;
    }

    private void SliderAutoCollapse_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblAutoCollapse != null)
        {
            LblAutoCollapse.Text = $"{e.NewValue:0.0}s";
        }
    }

    private void SliderAnimSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblAnimSpeed != null)
        {
            LblAnimSpeed.Text = $"{(int)e.NewValue}ms";
        }
    }

    private void SliderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblWidth != null)
        {
            LblWidth.Text = $"{(int)e.NewValue}px";
        }
    }

    private void SliderHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblHeight != null)
        {
            LblHeight.Text = $"{(int)e.NewValue}px";
        }
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblOpacity != null)
        {
            LblOpacity.Text = $"{(int)(e.NewValue * 100)}%";
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // Save back to settings
        _settings.EnableVolume = ChkVolume.IsChecked == true;
        _settings.EnableBrightness = ChkBrightness.IsChecked == true;
        _settings.EnableBattery = ChkBattery.IsChecked == true;
        _settings.EnableMedia = ChkMedia.IsChecked == true;
        _settings.EnableNotifications = ChkNotifications.IsChecked == true;
        _settings.EnableClipboard = ChkClipboard.IsChecked == true;

        _settings.ExpandOnHover = ChkExpandOnHover.IsChecked == true;
        _settings.ExpandOnClick = ChkExpandOnClick.IsChecked == true;
        _settings.AutoCollapseSeconds = SliderAutoCollapse.Value;
        _settings.AnimationSpeedMs = (int)SliderAnimSpeed.Value;

        _settings.IslandWidth = SliderWidth.Value;
        _settings.IslandHeight = SliderHeight.Value;
        _settings.CornerRadius = _settings.IslandHeight / 2.0;
        _settings.Opacity = SliderOpacity.Value;

        _settings.StartWithWindows = ChkStartup.IsChecked == true;

        _settings.Save();
        _onSettingsSaved(_settings);
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show(
            "Dynamic Island for Windows\n\n" +
            "A lightweight Windows desktop utility.\n\n" +
            "Version: 1.0.0\n" +
            "Made by Sagar",
            "About Dynamic Island",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
