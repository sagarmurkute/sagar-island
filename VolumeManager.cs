using System;
using NAudio.CoreAudioApi;

namespace SagarIsland;

public class VolumeChangedEventArgs : EventArgs
{
    public float MasterVolume { get; }
    public int VolumePercent => (int)Math.Round(MasterVolume * 100);
    public bool IsMuted { get; }

    public VolumeChangedEventArgs(float masterVolume, bool isMuted)
    {
        MasterVolume = masterVolume;
        IsMuted = isMuted;
    }
}

public class VolumeManager : IDisposable
{
    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    private MMDeviceEnumerator? _deviceEnumerator;
    private MMDevice? _defaultDevice;
    private bool _isDisposed;

    public void Initialize()
    {
        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                _defaultDevice.AudioEndpointVolume.OnVolumeNotification += AudioEndpointVolume_OnVolumeNotification;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VolumeManager] Init error: {ex.Message}");
        }
    }

    private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
    {
        VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(data.MasterVolume, data.Muted));
    }

    public float GetCurrentVolume()
    {
        try
        {
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                return _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            }
        }
        catch { }
        return 0.5f;
    }

    public bool GetIsMuted()
    {
        try
        {
            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                return _defaultDevice.AudioEndpointVolume.Mute;
            }
        }
        catch { }
        return false;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            try
            {
                if (_defaultDevice?.AudioEndpointVolume != null)
                {
                    _defaultDevice.AudioEndpointVolume.OnVolumeNotification -= AudioEndpointVolume_OnVolumeNotification;
                }
                _defaultDevice?.Dispose();
                _deviceEnumerator?.Dispose();
            }
            catch { }
            _isDisposed = true;
        }
    }
}
