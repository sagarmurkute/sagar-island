using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using LinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using DrawingVisual = System.Windows.Media.DrawingVisual;
using ScaleTransform = System.Windows.Media.ScaleTransform;
using PixelFormats = System.Windows.Media.PixelFormats;
using FormattedText = System.Windows.Media.FormattedText;
using Typeface = System.Windows.Media.Typeface;
using FlowDirection = System.Windows.FlowDirection;

namespace SagarIsland;

public static class IconGenerator
{
    public static void GenerateAll(string assetsDir)
    {
        Directory.CreateDirectory(assetsDir);

        // 1. App Icon PNGs and Multi-Resolution ICO
        int[] sizes = new int[] { 512, 256, 128, 64, 48, 32, 24, 16 };
        var pngBytesList = new Dictionary<int, byte[]>();

        foreach (int sz in sizes)
        {
            byte[] png = RenderIconToPng(sz);
            string pngPath = Path.Combine(assetsDir, $"icon-{sz}.png");
            File.WriteAllBytes(pngPath, png);
            pngBytesList[sz] = png;
        }

        File.WriteAllBytes(Path.Combine(assetsDir, "logo.png"), pngBytesList[512]);
        File.WriteAllBytes(Path.Combine(assetsDir, "avatar.png"), pngBytesList[512]);

        int[] icoSizes = new int[] { 256, 128, 64, 48, 32, 24, 16 };
        string icoPath = Path.Combine(assetsDir, "app.ico");
        BuildIcoFile(icoSizes, pngBytesList, icoPath);
        File.Copy(icoPath, Path.Combine(Directory.GetCurrentDirectory(), "app.ico"), true);

        // 2. Web Favicon ICO (16, 32, 48)
        int[] faviconSizes = new int[] { 48, 32, 16 };
        string faviconPath = Path.Combine(assetsDir, "favicon.ico");
        BuildIcoFile(faviconSizes, pngBytesList, faviconPath);

        // 3. System Tray Icons (Standard & High-Contrast Monochrome)
        var trayMap = new Dictionary<int, byte[]>();
        byte[] tray16 = RenderTrayIcon(16, false);
        byte[] tray32 = RenderTrayIcon(32, false);
        File.WriteAllBytes(Path.Combine(assetsDir, "tray-16.png"), tray16);
        File.WriteAllBytes(Path.Combine(assetsDir, "tray-32.png"), tray32);
        trayMap[16] = tray16;
        trayMap[32] = tray32;
        BuildIcoFile(new int[] { 32, 16 }, trayMap, Path.Combine(assetsDir, "tray.ico"));

        byte[] trayMono16 = RenderTrayIcon(16, true);
        byte[] trayMono32 = RenderTrayIcon(32, true);
        File.WriteAllBytes(Path.Combine(assetsDir, "tray-mono-16.png"), trayMono16);
        File.WriteAllBytes(Path.Combine(assetsDir, "tray-mono-32.png"), trayMono32);

        // 4. Windows Store / Start Menu Assets
        byte[] sq150 = RenderIconToPng(150);
        byte[] sq44 = RenderIconToPng(44);
        File.WriteAllBytes(Path.Combine(assetsDir, "Square150x150Logo.png"), sq150);
        File.WriteAllBytes(Path.Combine(assetsDir, "Square44x44Logo.png"), sq44);

        // 5. Installer Wizard Assets (PNG & BMP for Inno Setup)
        byte[] instHeader = RenderInstallerHeader(150, 57);
        byte[] instSidebar = RenderInstallerSidebar(164, 314);
        File.WriteAllBytes(Path.Combine(assetsDir, "installer-header.png"), instHeader);
        File.WriteAllBytes(Path.Combine(assetsDir, "installer-sidebar.png"), instSidebar);

        byte[] instHeaderBmp = RenderInstallerHeaderBmp(150, 57);
        byte[] instSidebarBmp = RenderInstallerSidebarBmp(164, 314);
        File.WriteAllBytes(Path.Combine(assetsDir, "installer-header.bmp"), instHeaderBmp);
        File.WriteAllBytes(Path.Combine(assetsDir, "installer-sidebar.bmp"), instSidebarBmp);

        // 6. GitHub & Documentation Hero Banner (1280x640)
        byte[] banner = RenderBanner(1280, 640);
        File.WriteAllBytes(Path.Combine(assetsDir, "banner.png"), banner);

        Console.WriteLine("All assets generated successfully.");
    }

    private static byte[] RenderIconToPng(int size)
    {
        double scale = size / 512.0;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));

            // Background Squircle
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#1A85F0")!,
                (Color)ColorConverter.ConvertFromString("#1875DC")!,
                new Point(0, 0),
                new Point(0, 1)
            );

            var shadowBrush = new SolidColorBrush(Color.FromArgb(90, 10, 60, 120));
            dc.DrawRoundedRectangle(shadowBrush, null, new Rect(32, 38, 448, 448), 104, 104);
            dc.DrawRoundedRectangle(bgBrush, null, new Rect(32, 32, 448, 448), 104, 104);

            // Glass Rim Stroke
            var rimPen = new Pen(new SolidColorBrush(Color.FromArgb(64, 255, 255, 255)), 1.5);
            dc.DrawRoundedRectangle(null, rimPen, new Rect(32.5, 32.5, 447, 447), 103.5, 103.5);

            // Main Dynamic Island Pill
            var islandBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#141416")!,
                (Color)ColorConverter.ConvertFromString("#050507")!,
                new Point(0, 0),
                new Point(0, 1)
            );

            dc.DrawRoundedRectangle(islandBrush, null, new Rect(96, 196, 320, 120), 60, 60);

            var pillRimPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 1.0);
            dc.DrawRoundedRectangle(null, pillRimPen, new Rect(96.5, 196.5, 319, 119), 59.5, 59.5);

            // Glowing Glassmorphic Top Line
            var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 3.0);
            dc.DrawLine(glowPen, new Point(140, 197), new Point(372, 197));

            var corePen = new Pen(new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)), 1.5);
            dc.DrawLine(corePen, new Point(140, 197), new Point(372, 197));

            // Left Live Indicator Dot
            var greenColor = (Color)ColorConverter.ConvertFromString("#00E676")!;
            var haloBrush = new SolidColorBrush(Color.FromArgb(70, 0, 230, 118));
            var dotBrush = new SolidColorBrush(greenColor);

            dc.DrawEllipse(haloBrush, null, new Point(156, 256), 22, 22);
            dc.DrawEllipse(dotBrush, null, new Point(156, 256), 14, 14);

            // Center Equalizer Waveform Bars
            var white90 = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
            var white60 = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
            var white75 = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255));
            var white50 = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));

            dc.DrawRoundedRectangle(white90, null, new Rect(232, 242, 6, 28), 3, 3);
            dc.DrawRoundedRectangle(white60, null, new Rect(244, 248, 6, 16), 3, 3);
            dc.DrawRoundedRectangle(dotBrush, null, new Rect(256, 236, 6, 40), 3, 3);
            dc.DrawRoundedRectangle(white75, null, new Rect(268, 244, 6, 24), 3, 3);
            dc.DrawRoundedRectangle(white50, null, new Rect(280, 250, 6, 12), 3, 3);

            // Right Live Activity Dynamic Ring
            var ringPenBg = new Pen(new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)), 4.0);
            dc.DrawEllipse(null, ringPenBg, new Point(356, 256), 16, 16);

            var greenPen = new Pen(dotBrush, 4.0);
            dc.DrawEllipse(null, greenPen, new Point(356, 256), 16, 16);
            dc.DrawEllipse(dotBrush, null, new Point(356, 256), 6, 6);

            dc.Pop();
        }

        return EncodeVisual(visual, size, size);
    }

    private static byte[] RenderTrayIcon(int size, bool monochrome)
    {
        double scale = size / 32.0;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));

            // Clean Tray Pill Capsule
            var pillBrush = monochrome
                ? new SolidColorBrush(Color.FromRgb(240, 240, 240))
                : new SolidColorBrush(Color.FromRgb(24, 24, 27));

            dc.DrawRoundedRectangle(pillBrush, null, new Rect(2, 8, 28, 16), 8, 8);

            if (!monochrome)
            {
                var strokePen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 0.75);
                dc.DrawRoundedRectangle(null, strokePen, new Rect(2, 8, 28, 16), 8, 8);

                // Green status dot
                var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
                dc.DrawEllipse(greenBrush, null, new Point(8, 16), 2.5, 2.5);

                // Sound waves
                var whiteBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                dc.DrawRoundedRectangle(whiteBrush, null, new Rect(14, 13, 1.5, 6), 0.75, 0.75);
                dc.DrawRoundedRectangle(greenBrush, null, new Rect(17, 11, 1.5, 10), 0.75, 0.75);
                dc.DrawRoundedRectangle(whiteBrush, null, new Rect(20, 14, 1.5, 4), 0.75, 0.75);
                dc.DrawRoundedRectangle(whiteBrush, null, new Rect(23, 12, 1.5, 8), 0.75, 0.75);
            }
            else
            {
                // Dark punch-through inside white pill
                var darkBrush = new SolidColorBrush(Color.FromRgb(16, 16, 16));
                dc.DrawEllipse(darkBrush, null, new Point(8, 16), 2.5, 2.5);
                dc.DrawRoundedRectangle(darkBrush, null, new Rect(14, 13, 1.5, 6), 0.75, 0.75);
                dc.DrawRoundedRectangle(darkBrush, null, new Rect(17, 11, 1.5, 10), 0.75, 0.75);
                dc.DrawRoundedRectangle(darkBrush, null, new Rect(20, 14, 1.5, 4), 0.75, 0.75);
                dc.DrawRoundedRectangle(darkBrush, null, new Rect(23, 12, 1.5, 8), 0.75, 0.75);
            }

            dc.Pop();
        }

        return EncodeVisual(visual, size, size);
    }

    private static byte[] RenderInstallerHeader(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#1A85F0")!,
                (Color)ColorConverter.ConvertFromString("#1875DC")!,
                new Point(0, 0),
                new Point(1, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            // Small Island Badge on the right
            var islandBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0C")!);
            dc.DrawRoundedRectangle(islandBrush, null, new Rect(width - 54, 14, 44, 28), 14, 14);

            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
            dc.DrawEllipse(greenBrush, null, new Point(width - 44, 28), 3.5, 3.5);

            var whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(width - 34, 25, 2, 6), 1, 1);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(width - 30, 23, 2, 10), 1, 1);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(width - 26, 26, 2, 4), 1, 1);
        }

        return EncodeVisual(visual, width, height);
    }

    private static byte[] RenderInstallerSidebar(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#1A85F0")!,
                (Color)ColorConverter.ConvertFromString("#0F4C81")!,
                new Point(0, 0),
                new Point(0, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            // Floating Island Graphic in upper section
            var islandBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0C")!);
            dc.DrawRoundedRectangle(islandBrush, null, new Rect(22, 60, 120, 52), 26, 26);

            var strokePen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1.0);
            dc.DrawRoundedRectangle(null, strokePen, new Rect(22, 60, 120, 52), 26, 26);

            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
            dc.DrawEllipse(greenBrush, null, new Point(44, 86), 6, 6);

            var whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(68, 80, 3, 12), 1.5, 1.5);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(75, 75, 3, 22), 1.5, 1.5);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(82, 82, 3, 8), 1.5, 1.5);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(89, 78, 3, 16), 1.5, 1.5);

            dc.DrawEllipse(null, new Pen(greenBrush, 2.0), new Point(118, 86), 8, 8);
            dc.DrawEllipse(greenBrush, null, new Point(118, 86), 3, 3);
        }

        return EncodeVisual(visual, width, height);
    }

    private static byte[] RenderBanner(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Canvas Background
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#0B0B0E")!,
                (Color)ColorConverter.ConvertFromString("#141418")!,
                new Point(0, 0),
                new Point(1, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            // Ambient Glow behind island
            var glowBrush = new SolidColorBrush(Color.FromArgb(35, 26, 133, 240));
            dc.DrawEllipse(glowBrush, null, new Point(width / 2.0, 240), 320, 140);

            // Large Hero Floating Dynamic Island (Center)
            double pillW = 560;
            double pillH = 110;
            double pillX = (width - pillW) / 2.0;
            double pillY = 185;

            var islandBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#18181B")!,
                (Color)ColorConverter.ConvertFromString("#09090B")!,
                new Point(0, 0),
                new Point(0, 1)
            );
            dc.DrawRoundedRectangle(islandBrush, null, new Rect(pillX, pillY, pillW, pillH), 55, 55);

            var pillPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 1.0);
            dc.DrawRoundedRectangle(null, pillPen, new Rect(pillX, pillY, pillW, pillH), 55, 55);

            // Specular Top Line
            var specPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1.5);
            dc.DrawLine(specPen, new Point(pillX + 80, pillY + 1), new Point(pillX + pillW - 80, pillY + 1));

            // Island Internal Content
            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
            var greenHalo = new SolidColorBrush(Color.FromArgb(60, 0, 230, 118));
            dc.DrawEllipse(greenHalo, null, new Point(pillX + 60, pillY + 55), 20, 20);
            dc.DrawEllipse(greenBrush, null, new Point(pillX + 60, pillY + 55), 12, 12);

            // Equalizer
            var whiteBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(pillX + 220, pillY + 42, 6, 26), 3, 3);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(pillX + 234, pillY + 48, 6, 14), 3, 3);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(pillX + 248, pillY + 36, 6, 38), 3, 3);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(pillX + 262, pillY + 44, 6, 22), 3, 3);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(pillX + 276, pillY + 50, 6, 10), 3, 3);

            // Dynamic Ring
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 4), new Point(pillX + pillW - 60, pillY + 55), 18, 18);
            dc.DrawEllipse(null, new Pen(greenBrush, 4), new Point(pillX + pillW - 60, pillY + 55), 18, 18);
            dc.DrawEllipse(greenBrush, null, new Point(pillX + pillW - 60, pillY + 55), 7, 7);
        }

        return EncodeVisual(visual, width, height);
    }

    private static byte[] RenderInstallerHeaderBmp(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#1A85F0")!,
                (Color)ColorConverter.ConvertFromString("#1875DC")!,
                new Point(0, 0),
                new Point(1, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            var islandBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0C")!);
            dc.DrawRoundedRectangle(islandBrush, null, new Rect(width - 54, 14, 44, 28), 14, 14);

            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
            dc.DrawEllipse(greenBrush, null, new Point(width - 44, 28), 3.5, 3.5);

            var whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(width - 34, 25, 2, 6), 1, 1);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(width - 30, 23, 2, 10), 1, 1);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(width - 26, 26, 2, 4), 1, 1);
        }

        return EncodeVisualBmp(visual, width, height);
    }

    private static byte[] RenderInstallerSidebarBmp(int width, int height)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#1A85F0")!,
                (Color)ColorConverter.ConvertFromString("#0F4C81")!,
                new Point(0, 0),
                new Point(0, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            var islandBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0C")!);
            dc.DrawRoundedRectangle(islandBrush, null, new Rect(22, 60, 120, 52), 26, 26);

            var strokePen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1.0);
            dc.DrawRoundedRectangle(null, strokePen, new Rect(22, 60, 120, 52), 26, 26);

            var greenBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676")!);
            dc.DrawEllipse(greenBrush, null, new Point(44, 86), 6, 6);

            var whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(68, 80, 3, 12), 1.5, 1.5);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(75, 75, 3, 22), 1.5, 1.5);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(82, 82, 3, 8), 1.5, 1.5);
            dc.DrawRoundedRectangle(whiteBrush, null, new Rect(89, 78, 3, 16), 1.5, 1.5);

            dc.DrawEllipse(null, new Pen(greenBrush, 2.0), new Point(118, 86), 8, 8);
            dc.DrawEllipse(greenBrush, null, new Point(118, 86), 3, 3);
        }

        return EncodeVisualBmp(visual, width, height);
    }

    private static byte[] EncodeVisualBmp(DrawingVisual visual, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (var ms = new MemoryStream())
        {
            encoder.Save(ms);
            return ms.ToArray();
        }
    }

    private static byte[] EncodeVisual(DrawingVisual visual, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (var ms = new MemoryStream())
        {
            encoder.Save(ms);
            return ms.ToArray();
        }
    }

    private static void BuildIcoFile(int[] sizes, Dictionary<int, byte[]> pngMap, string outputPath)
    {
        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((ushort)0); // Reserved
            bw.Write((ushort)1); // Type: 1 = ICO
            bw.Write((ushort)sizes.Length); // Image count

            int offset = 6 + (16 * sizes.Length);

            foreach (int sz in sizes)
            {
                byte[] pngData = pngMap[sz];
                bw.Write((byte)(sz >= 256 ? 0 : sz));
                bw.Write((byte)(sz >= 256 ? 0 : sz));
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write((uint)pngData.Length);
                bw.Write((uint)offset);

                offset += pngData.Length;
            }

            foreach (int sz in sizes)
            {
                bw.Write(pngMap[sz]);
            }
        }
    }
}
