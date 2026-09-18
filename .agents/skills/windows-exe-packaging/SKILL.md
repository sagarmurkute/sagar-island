---
name: windows-exe-packaging
description: Guidelines and scripts for building and packaging standalone Windows .exe binaries with electron-builder or cargo/tauri.
---

# Windows Executable Packaging Guidelines

## 1. Standalone Exe Targets
- **Portable .exe**: Single executable file that runs without installation.
- **NSIS Installer**: Standard Windows setup wizard with auto-update capability and desktop/start menu shortcuts.

## 2. Configuration Best Practices
- Define `appId`, `productName`, and `win.icon` (`.ico` format with multi-resolution 16x16 up to 256x256).
- Set `requestedExecutionLevel: "asInvoker"` (avoids unnecessary UAC prompts).
- Asar bundling enabled with native module unpacking where applicable.
- Startup registry integration (Run on Windows Boot option).
