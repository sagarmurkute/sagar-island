---
name: windows-island-core
description: Core architecture and best practices for creating a frameless, transparent, always-on-top Dynamic Island overlay for Windows.
---

# Windows Dynamic Island Core Architecture

## 1. Window Configuration
- **Frameless & Transparent**: Window must have `frame: false`, `transparent: true`, and `hasShadow: false` (custom CSS glow/shadows).
- **Positioning**: Top center of primary screen, offset by 0-8px from the top bezel.
- **Z-Index**: `alwaysOnTop: true` with level `screen-saver` or `floating`.
- **Taskbar Exclusion**: `skipTaskbar: true` to act as a system UI element rather than a standard window.

## 2. Interactive Region / Hit-Testing (Click-Through)
- Transparent background regions outside the island pill must be click-through (`setIgnoreMouseEvents(true, { forward: true })`).
- When the cursor enters the active island surface, re-enable mouse events (`setIgnoreMouseEvents(false)`).

## 3. System Integrations
- **System Tray**: Tray icon with right-click context menu (Settings, Pause, Quit, Position Reset).
- **Windows Media Session**: Track title, artist, playback state, album art via SMTC or local media APIs.
- **Hardware Telemetry**: CPU, RAM, Battery, Volume HUD indicators.
