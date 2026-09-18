using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace SagarIsland;

public class MediaSessionChangedEventArgs : EventArgs
{
    public string Title { get; }
    public string Artist { get; }
    public bool IsPlaying { get; }
    public byte[]? ThumbnailBytes { get; }
    public bool HasMedia { get; }
    public TimeSpan Position { get; }
    public TimeSpan Duration { get; }

    public MediaSessionChangedEventArgs(string title, string artist, bool isPlaying, byte[]? thumbnailBytes, bool hasMedia = true, TimeSpan? position = null, TimeSpan? duration = null)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Unknown Track" : title;
        Artist = string.IsNullOrWhiteSpace(artist) ? "Unknown Artist" : artist;
        IsPlaying = isPlaying;
        ThumbnailBytes = thumbnailBytes;
        HasMedia = hasMedia;
        Position = position ?? TimeSpan.Zero;
        Duration = duration ?? TimeSpan.Zero;
    }
}

public class MediaManager : IDisposable
{
    public event EventHandler<MediaSessionChangedEventArgs>? MediaChanged;

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _isDisposed = false;

    // Win32 Media Keys Simulation Fallback
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const int KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private static void SendMediaKey(byte vk)
    {
        try
        {
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        catch { }
    }

    public async void Initialize()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                _sessionManager.SessionsChanged += SessionManager_SessionsChanged;
                HookCurrentSession();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MediaManager] Init error: {ex.Message}");
        }
    }

    private void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        HookCurrentSession();
    }

    private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        HookCurrentSession();
    }

    public void HookCurrentSession()
    {
        if (_sessionManager == null) return;

        try
        {
            if (_currentSession != null)
            {
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
            }

            _currentSession = _sessionManager.GetCurrentSession();

            // If current session is null, try to find any active session with playing or recent status
            if (_currentSession == null)
            {
                var sessions = _sessionManager.GetSessions();
                if (sessions != null && sessions.Count > 0)
                {
                    foreach (var s in sessions)
                    {
                        var info = s.GetPlaybackInfo();
                        if (info != null && info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                        {
                            _currentSession = s;
                            break;
                        }
                    }
                    if (_currentSession == null)
                    {
                        _currentSession = sessions[0];
                    }
                }
            }

            if (_currentSession != null)
            {
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
                UpdateMediaInfo();
            }
            else
            {
                MediaChanged?.Invoke(this, new MediaSessionChangedEventArgs("", "", false, null, hasMedia: false));
            }
        }
        catch { }
    }

    private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        UpdateMediaInfo();
    }

    private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        UpdateMediaInfo();
    }

    private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        UpdateMediaInfo();
    }

    public async void UpdateMediaInfo()
    {
        if (_currentSession == null)
        {
            MediaChanged?.Invoke(this, new MediaSessionChangedEventArgs("", "", false, null, hasMedia: false));
            return;
        }

        try
        {
            var playbackInfo = _currentSession.GetPlaybackInfo();
            var isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var mediaProps = await _currentSession.TryGetMediaPropertiesAsync();
            if (mediaProps != null && !string.IsNullOrWhiteSpace(mediaProps.Title))
            {
                byte[]? thumbBytes = null;
                if (mediaProps.Thumbnail != null)
                {
                    try
                    {
                        using var stream = await mediaProps.Thumbnail.OpenReadAsync();
                        using var netStream = stream.AsStreamForRead();
                        using var ms = new MemoryStream();
                        await netStream.CopyToAsync(ms);
                        thumbBytes = ms.ToArray();
                    }
                    catch { }
                }

                TimeSpan pos = TimeSpan.Zero;
                TimeSpan dur = TimeSpan.Zero;
                try
                {
                    var timeline = _currentSession.GetTimelineProperties();
                    if (timeline != null)
                    {
                        pos = timeline.Position;
                        dur = timeline.EndTime;
                    }
                }
                catch { }

                MediaChanged?.Invoke(this, new MediaSessionChangedEventArgs(mediaProps.Title, mediaProps.Artist, isPlaying, thumbBytes, hasMedia: true, position: pos, duration: dur));
            }
            else
            {
                MediaChanged?.Invoke(this, new MediaSessionChangedEventArgs("", "", false, null, hasMedia: false));
            }
        }
        catch
        {
            MediaChanged?.Invoke(this, new MediaSessionChangedEventArgs("", "", false, null, hasMedia: false));
        }
    }

    public async Task<bool> TogglePlayPauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                bool success = await _currentSession.TryTogglePlayPauseAsync();
                if (success) return true;
            }
            catch { }
        }

        SendMediaKey(VK_MEDIA_PLAY_PAUSE);
        return true;
    }

    public async Task<bool> NextTrackAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                bool success = await _currentSession.TrySkipNextAsync();
                if (success) return true;
            }
            catch { }
        }

        SendMediaKey(VK_MEDIA_NEXT_TRACK);
        return true;
    }

    public async Task<bool> PrevTrackAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                bool success = await _currentSession.TrySkipPreviousAsync();
                if (success) return true;
            }
            catch { }
        }

        SendMediaKey(VK_MEDIA_PREV_TRACK);
        return true;
    }

    public async Task<bool> SkipSecondsAsync(double seconds)
    {
        if (_currentSession != null)
        {
            try
            {
                var timeline = _currentSession.GetTimelineProperties();
                if (timeline != null)
                {
                    var targetPos = timeline.Position + TimeSpan.FromSeconds(seconds);
                    if (targetPos < TimeSpan.Zero) targetPos = TimeSpan.Zero;
                    if (targetPos > timeline.EndTime) targetPos = timeline.EndTime;
                    return await _currentSession.TryChangePlaybackPositionAsync((long)targetPos.Ticks);
                }
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> SeekToPercentAsync(double percent)
    {
        if (_currentSession != null)
        {
            try
            {
                var timeline = _currentSession.GetTimelineProperties();
                if (timeline != null && timeline.EndTime.TotalSeconds > 0)
                {
                    double targetSec = timeline.EndTime.TotalSeconds * Math.Clamp(percent, 0.0, 1.0);
                    var targetPos = TimeSpan.FromSeconds(targetSec);
                    return await _currentSession.TryChangePlaybackPositionAsync((long)targetPos.Ticks);
                }
            }
            catch { }
        }
        return false;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged -= SessionManager_CurrentSessionChanged;
                _sessionManager.SessionsChanged -= SessionManager_SessionsChanged;
            }
            if (_currentSession != null)
            {
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
            }
            _isDisposed = true;
        }
    }
}
