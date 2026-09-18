using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace SagarIsland;

public enum ClipboardContentType
{
    Text,
    Link,
    Image,
    Files
}

public class ClipboardChangedEventArgs : EventArgs
{
    public ClipboardContentType ContentType { get; }
    public string Summary { get; }
    public string Detail { get; }

    public ClipboardChangedEventArgs(ClipboardContentType contentType, string summary, string detail)
    {
        ContentType = contentType;
        Summary = summary;
        Detail = detail;
    }
}

public class ClipboardManager : IDisposable
{
    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

    private const int WM_CLIPBOARDUPDATE = 0x031D;
    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private string _lastCopiedSignature = "";
    private DateTime _lastCopiedTime = DateTime.MinValue;
    private bool _isDisposed = false;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    public void Initialize(Window window)
    {
        try
        {
            _hwnd = new WindowInteropHelper(window).Handle;
            if (_hwnd != IntPtr.Zero)
            {
                _hwndSource = HwndSource.FromHwnd(_hwnd);
                _hwndSource?.AddHook(WndProc);
                AddClipboardFormatListener(_hwnd);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClipboardManager] Init error: {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            OnClipboardUpdate();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardUpdate()
    {
        // Debounce
        if ((DateTime.UtcNow - _lastCopiedTime).TotalMilliseconds < 250)
        {
            return;
        }

        // Retry loop for clipboard read because copying apps temporarily lock clipboard
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string text = System.Windows.Clipboard.GetText();
                    if (string.IsNullOrWhiteSpace(text) || text == _lastCopiedSignature)
                    {
                        return;
                    }

                    _lastCopiedSignature = text;
                    _lastCopiedTime = DateTime.UtcNow;

                    string cleanText = text.Trim().Replace("\r\n", " ").Replace("\n", " ");
                    bool isUrl = cleanText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                 cleanText.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                 cleanText.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

                    string snippet = cleanText.Length > 36 ? cleanText.Substring(0, 33) + "..." : cleanText;

                    if (isUrl)
                    {
                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(ClipboardContentType.Link, "Link copied", snippet));
                    }
                    else
                    {
                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(ClipboardContentType.Text, "Copied", snippet));
                    }
                    return;
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    var files = System.Windows.Clipboard.GetFileDropList();
                    if (files != null && files.Count > 0)
                    {
                        string sig = $"FILES_{files.Count}_{files[0]}";
                        if (sig == _lastCopiedSignature) return;

                        _lastCopiedSignature = sig;
                        _lastCopiedTime = DateTime.UtcNow;

                        int count = files.Count;
                        string summary = count == 1 ? "1 file copied" : $"{count} files copied";
                        string firstFileName = Path.GetFileName(files[0] ?? "");
                        string detail = count == 1 ? firstFileName : $"{firstFileName} + {count - 1} more";

                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(ClipboardContentType.Files, summary, detail));
                        return;
                    }
                }
                else if (System.Windows.Clipboard.ContainsImage())
                {
                    string sig = $"IMAGE_{DateTime.UtcNow.Ticks}";
                    _lastCopiedSignature = sig;
                    _lastCopiedTime = DateTime.UtcNow;

                    ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(ClipboardContentType.Image, "Image copied", "Bitmap Graphic"));
                    return;
                }

                break;
            }
            catch
            {
                Thread.Sleep(30);
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_hwnd != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_hwnd);
                _hwndSource?.RemoveHook(WndProc);
            }
            _isDisposed = true;
        }
    }
}
