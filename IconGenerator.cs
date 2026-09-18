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
using RadialGradientBrush = System.Windows.Media.RadialGradientBrush;
using GradientStopCollection = System.Windows.Media.GradientStopCollection;
using GradientStop = System.Windows.Media.GradientStop;
using BrushConverter = System.Windows.Media.BrushConverter;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyles = System.Windows.FontStyles;
using FontWeights = System.Windows.FontWeights;
using FontStretches = System.Windows.FontStretches;
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
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            // 1. Deep Obsidian Slate Background Canvas
            var bgBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#08090D")!,
                (Color)ColorConverter.ConvertFromString("#0E1118")!,
                new Point(0, 0),
                new Point(1, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, width, height));

            // Ambient Glow Blobs
            var blueGlow = new RadialGradientBrush
            {
                Center = new Point(0.25, 0.2),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(45, 26, 133, 240), 0.0),
                    new GradientStop(Color.FromArgb(0, 26, 133, 240), 1.0)
                }
            };
            dc.DrawRectangle(blueGlow, null, new Rect(0, 0, width, height));

            var greenGlow = new RadialGradientBrush
            {
                Center = new Point(0.75, 0.75),
                RadiusX = 0.5,
                RadiusY = 0.5,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(30, 0, 230, 118), 0.0),
                    new GradientStop(Color.FromArgb(0, 0, 230, 118), 1.0)
                }
            };
            dc.DrawRectangle(greenGlow, null, new Rect(0, 0, width, height));

            // Ambient Center Shadow / Platform
            var centerGlow = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.58),
                RadiusX = 0.55,
                RadiusY = 0.45,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(40, 0, 150, 255), 0.0),
                    new GradientStop(Color.FromArgb(0, 10, 15, 30), 1.0)
                }
            };
            dc.DrawRectangle(centerGlow, null, new Rect(0, 0, width, height));

            // 2. Header Typography
            var titleTypeface = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var subtitleTypeface = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var badgeTypeface = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

            // Category Pill Tag
            var tagBg = new SolidColorBrush(Color.FromArgb(35, 26, 133, 240));
            var tagBorder = new Pen(new SolidColorBrush(Color.FromArgb(70, 26, 133, 240)), 1.0);
            dc.DrawRoundedRectangle(tagBg, tagBorder, new Rect(width / 2.0 - 140, 36, 280, 26), 13, 13);

            var tagText = new FormattedText("✦ DYNAMIC ISLAND FOR WINDOWS", culture, FlowDirection.LeftToRight, badgeTypeface, 11, (SolidColorBrush)new BrushConverter().ConvertFromString("#70E1F5")!, 1.0);
            dc.DrawText(tagText, new Point(width / 2.0 - tagText.Width / 2.0, 42));

            // Main Title
            var titleText = new FormattedText("Sagar Island", culture, FlowDirection.LeftToRight, titleTypeface, 36, (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!, 1.0);
            dc.DrawText(titleText, new Point(width / 2.0 - titleText.Width / 2.0, 68));

            // Subtitle
            var subText = new FormattedText("Fluid desktop companion with live media controls, hardware HUDs & spring physics", culture, FlowDirection.LeftToRight, subtitleTypeface, 14, (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!, 1.0);
            dc.DrawText(subText, new Point(width / 2.0 - subText.Width / 2.0, 114));

            // 3. Central Hero Feature Card: Rich Media Expanded Player (500x170)
            double cardW = 500;
            double cardH = 170;
            double cardX = (width - cardW) / 2.0;
            double cardY = 160;

            // Card Shadow
            var cardShadow = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0));
            dc.DrawRoundedRectangle(cardShadow, null, new Rect(cardX, cardY + 12, cardW, cardH), 24, 24);

            // Card Body
            var cardBg = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#141417")!,
                (Color)ColorConverter.ConvertFromString("#0A0A0C")!,
                new Point(0, 0),
                new Point(0, 1)
            );
            var cardBorder = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0.75);
            dc.DrawRoundedRectangle(cardBg, cardBorder, new Rect(cardX, cardY, cardW, cardH), 24, 24);

            // Specular Top Rim on Card
            var specRim = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0);
            dc.DrawLine(specRim, new Point(cardX + 40, cardY + 0.5), new Point(cardX + cardW - 40, cardY + 0.5));

            // Album Artwork Tile
            var artBg = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#8A2387")!,
                (Color)ColorConverter.ConvertFromString("#E94057")!,
                new Point(0, 0),
                new Point(1, 1)
            );
            dc.DrawRoundedRectangle(artBg, null, new Rect(cardX + 22, cardY + 22, 54, 54), 12, 12);
            // Artwork note icon
            var artNoteText = new FormattedText("♫", culture, FlowDirection.LeftToRight, titleTypeface, 24, (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!, 1.0);
            dc.DrawText(artNoteText, new Point(cardX + 38, cardY + 34));

            // Track Title & Artist
            var trackTitle = new FormattedText("Blinding Lights", culture, FlowDirection.LeftToRight, titleTypeface, 15, (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, 1.0);
            dc.DrawText(trackTitle, new Point(cardX + 90, cardY + 28));

            var trackArtist = new FormattedText("The Weeknd • After Hours", culture, FlowDirection.LeftToRight, subtitleTypeface, 12, (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!, 1.0);
            dc.DrawText(trackArtist, new Point(cardX + 90, cardY + 50));

            // 4-Bar Dancing Visualizer on right of title
            var greenBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#00E676")!;
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(cardX + cardW - 60, cardY + 34, 4, 22), 2, 2);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(cardX + cardW - 52, cardY + 42, 4, 14), 2, 2);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(cardX + cardW - 44, cardY + 28, 4, 28), 2, 2);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(cardX + cardW - 36, cardY + 38, 4, 18), 2, 2);

            // Progress Bar & Timestamps
            double pbX = cardX + 22;
            double pbY = cardY + 92;
            double pbW = cardW - 44;
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), null, new Rect(pbX, pbY, pbW, 4), 2, 2);
            dc.DrawRoundedRectangle((SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, null, new Rect(pbX, pbY, pbW * 0.42, 4), 2, 2);

            var timeCurrent = new FormattedText("01:24", culture, FlowDirection.LeftToRight, subtitleTypeface, 10, (SolidColorBrush)new BrushConverter().ConvertFromString("#555555")!, 1.0);
            dc.DrawText(timeCurrent, new Point(pbX, pbY + 7));

            var timeTotal = new FormattedText("03:45", culture, FlowDirection.LeftToRight, subtitleTypeface, 10, (SolidColorBrush)new BrushConverter().ConvertFromString("#555555")!, 1.0);
            dc.DrawText(timeTotal, new Point(pbX + pbW - 28, pbY + 7));

            // Media Controls (Replay10, Prev, Play/Pause, Next, Forward10)
            double btnCenterY = cardY + 138;
            double ctrlCenterX = cardX + cardW / 2.0;

            // Prev 10
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 28, 30)), null, new Rect(ctrlCenterX - 110, btnCenterY - 12, 24, 24), 12, 12);
            var txt10L = new FormattedText("↺", culture, FlowDirection.LeftToRight, badgeTypeface, 12, (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!, 1.0);
            dc.DrawText(txt10L, new Point(ctrlCenterX - 103, btnCenterY - 9));

            // Prev Track
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 28, 30)), null, new Rect(ctrlCenterX - 65, btnCenterY - 14, 28, 28), 14, 14);
            var txtPrev = new FormattedText("⏮", culture, FlowDirection.LeftToRight, badgeTypeface, 11, (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, 1.0);
            dc.DrawText(txtPrev, new Point(ctrlCenterX - 57, btnCenterY - 8));

            // Large White Play/Pause Button
            dc.DrawRoundedRectangle((SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!, null, new Rect(ctrlCenterX - 18, btnCenterY - 18, 36, 36), 18, 18);
            var txtPause = new FormattedText("⏸", culture, FlowDirection.LeftToRight, badgeTypeface, 14, (SolidColorBrush)new BrushConverter().ConvertFromString("#000000")!, 1.0);
            dc.DrawText(txtPause, new Point(ctrlCenterX - 7, btnCenterY - 10));

            // Next Track
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 28, 30)), null, new Rect(ctrlCenterX + 37, btnCenterY - 14, 28, 28), 14, 14);
            var txtNext = new FormattedText("⏭", culture, FlowDirection.LeftToRight, badgeTypeface, 11, (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, 1.0);
            dc.DrawText(txtNext, new Point(ctrlCenterX + 45, btnCenterY - 8));

            // Forward 10
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 28, 30)), null, new Rect(ctrlCenterX + 86, btnCenterY - 12, 24, 24), 12, 12);
            var txt10R = new FormattedText("↻", culture, FlowDirection.LeftToRight, badgeTypeface, 12, (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!, 1.0);
            dc.DrawText(txt10R, new Point(ctrlCenterX + 93, btnCenterY - 9));

            // 4. Floating Left HUD: Volume HUD (280x60)
            double hudLW = 280;
            double hudLH = 60;
            double hudLX = 60;
            double hudLY = 360;

            dc.DrawRoundedRectangle(cardShadow, null, new Rect(hudLX, hudLY + 10, hudLW, hudLH), 20, 20);
            dc.DrawRoundedRectangle(cardBg, cardBorder, new Rect(hudLX, hudLY, hudLW, hudLH), 20, 20);

            var blueBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#0A84FF")!;
            var volIcon = new FormattedText("🔊", culture, FlowDirection.LeftToRight, titleTypeface, 14, blueBrush, 1.0);
            dc.DrawText(volIcon, new Point(hudLX + 16, hudLY + 12));

            var volTitle = new FormattedText("Volume", culture, FlowDirection.LeftToRight, badgeTypeface, 13, (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, 1.0);
            dc.DrawText(volTitle, new Point(hudLX + 40, hudLY + 13));

            var volPct = new FormattedText("72%", culture, FlowDirection.LeftToRight, titleTypeface, 13, (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!, 1.0);
            dc.DrawText(volPct, new Point(hudLX + hudLW - 48, hudLY + 13));

            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), null, new Rect(hudLX + 16, hudLY + 38, hudLW - 32, 4), 2, 2);
            dc.DrawRoundedRectangle(blueBrush, null, new Rect(hudLX + 16, hudLY + 38, (hudLW - 32) * 0.72, 4), 2, 2);

            // 5. Floating Center-Right HUD: Battery / Charging HUD (260x56)
            double hudRW = 260;
            double hudRH = 56;
            double hudRX = width - hudRW - 60;
            double hudRY = 362;

            dc.DrawRoundedRectangle(cardShadow, null, new Rect(hudRX, hudRY + 10, hudRW, hudRH), 20, 20);
            dc.DrawRoundedRectangle(cardBg, cardBorder, new Rect(hudRX, hudRY, hudRW, hudRH), 20, 20);

            var batState = new FormattedText("Charging", culture, FlowDirection.LeftToRight, subtitleTypeface, 11, greenBrush, 1.0);
            dc.DrawText(batState, new Point(hudRX + 16, hudRY + 10));

            var batIcon = new FormattedText("🔋", culture, FlowDirection.LeftToRight, titleTypeface, 13, greenBrush, 1.0);
            dc.DrawText(batIcon, new Point(hudRX + 16, hudRY + 28));

            var batPct = new FormattedText("78%", culture, FlowDirection.LeftToRight, titleTypeface, 13, (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF")!, 1.0);
            dc.DrawText(batPct, new Point(hudRX + 38, hudRY + 28));

            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), null, new Rect(hudRX + hudRW - 88, hudRY + 33, 72, 4), 2, 2);
            dc.DrawRoundedRectangle(greenBrush, null, new Rect(hudRX + hudRW - 88, hudRY + 33, 72 * 0.78, 4), 2, 2);

            // 6. Floating Notification HUD (320x54) - Bottom Center
            double notifW = 340;
            double notifH = 54;
            double notifX = (width - notifW) / 2.0;
            double notifY = 460;

            dc.DrawRoundedRectangle(cardShadow, null, new Rect(notifX, notifY + 10, notifW, notifH), 20, 20);
            dc.DrawRoundedRectangle(cardBg, cardBorder, new Rect(notifX, notifY, notifW, notifH), 20, 20);

            // Notification Bell Badge
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 28, 30)), null, new Rect(notifX + 12, notifY + 11, 32, 32), 16, 16);
            var bellIcon = new FormattedText("🔔", culture, FlowDirection.LeftToRight, titleTypeface, 14, (SolidColorBrush)new BrushConverter().ConvertFromString("#FF9F0A")!, 1.0);
            dc.DrawText(bellIcon, new Point(notifX + 21, notifY + 18));

            var notifApp = new FormattedText("Discord", culture, FlowDirection.LeftToRight, subtitleTypeface, 11, (SolidColorBrush)new BrushConverter().ConvertFromString("#8E8E93")!, 1.0);
            dc.DrawText(notifApp, new Point(notifX + 54, notifY + 11));

            var notifTitle = new FormattedText("New message received", culture, FlowDirection.LeftToRight, badgeTypeface, 13, (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6")!, 1.0);
            dc.DrawText(notifTitle, new Point(notifX + 54, notifY + 27));

            // Footer brand signature
            var footText = new FormattedText("Open Source • Windows 11 & 10 • Native .NET 8 WPF", culture, FlowDirection.LeftToRight, subtitleTypeface, 11, (SolidColorBrush)new BrushConverter().ConvertFromString("#444448")!, 1.0);
            dc.DrawText(footText, new Point(width / 2.0 - footText.Width / 2.0, height - 32));
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
