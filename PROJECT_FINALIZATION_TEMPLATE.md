# 🚀 Master Project Finalization & Launch Playbook Template
> **A complete, repeatable, step-by-step blueprint to take any project from local development code to a world-class, production-grade, open-source GitHub repository.**

---

## 📋 Executive Overview

Use this template as your universal checklist and blueprint when finishing and launching any software project. It covers everything from code hygiene and programmatic asset generation to automated CI/CD releases and package manager distribution.

---

## 📑 Table of Contents
1. [Phase 1: Repository Hygiene & Workspace Cleansing](#phase-1-repository-hygiene--workspace-cleansing)
2. [Phase 2: Visual Identity & Asset Generation](#phase-2-visual-identity--asset-generation)
3. [Phase 3: Standalone Packaging & Installer Compilation](#phase-3-standalone-packaging--installer-compilation)
4. [Phase 4: CI/CD Automation & GitHub Workflows](#phase-4-cicd-automation--github-workflows)
5. [Phase 5: Community Governance & Issue/PR Infrastructure](#phase-5-community-governance--issuepr-infrastructure)
6. [Phase 6: Package Manager Distribution Manifests](#phase-6-package-manager-distribution-manifests)
7. [Phase 7: World-Class README & Showcase](#phase-7-world-class-readme--showcase)
8. [Phase 8: Tagging & Release Execution Checklist](#phase-8-tagging--release-execution-checklist)

---

## 🧹 Phase 1: Repository Hygiene & Workspace Cleansing

### 1.1 Strict `.gitignore` Setup
Ensure no temporary, build, or internal development artifacts pollute the repository.

```gitignore
# Build & Output Artifacts
bin/
obj/
publish/
dist/
dist-installer/
out/

# IDE & Editor Metadata
.vs/
.vscode/*
!.vscode/settings.json
!.vscode/tasks.json
!.vscode/launch.json
.idea/
*.suo
*.user

# System & OS Files
Thumbs.db
Desktop.ini
.DS_Store

# Agent & Internal Scratch Files
.agents/
FEATURE_CHECKLIST.md
*.tmp
*.log
```

### 1.2 Open-Source License
Always include an official `LICENSE` file (e.g. MIT, Apache 2.0, or GPL-3.0) with current year and author name:
```text
MIT License

Copyright (c) {{YEAR}} {{AUTHOR_NAME}}

Permission is hereby granted, free of charge, to any person obtaining a copy...
```

### 1.3 Remove Untracked Dev Files from Git
If internal files were accidentally tracked, remove them without deleting locally:
```powershell
git rm -r --cached .agents/ 2>$null
git rm --cached FEATURE_CHECKLIST.md 2>$null
```

---

## 🎨 Phase 2: Visual Identity & Asset Generation

### 2.1 Programmatic Icon & Asset Generator
Do not rely on raster upscaling or static images. Write a deterministic asset renderer script/tool (e.g. WPF/Skia/Cairo) that outputs:
- Multi-resolution `app.ico` (16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256)
- Tray / Notification icons (`tray.ico`, `tray-active.ico`, `tray-dark.ico`)
- Installer graphics:
  - `wizard-small.bmp` (55x58 px, 24-bit RGB)
  - `wizard-large.bmp` (164x314 px, 24-bit RGB)
- Full-fidelity Hero Showcase Banner (`Assets/banner.png`, 1280x420 px)

### 2.2 Hero Showcase Banner Checklist
A high-converting hero banner should include:
- [ ] Dark, modern gradient background (e.g. `#080B10` to `#161D2B`)
- [ ] Subtle ambient radial glow behind the product mockup
- [ ] 1:1 pixel-accurate UI preview with soft drop shadows
- [ ] Bold typography (Title, Tagline, Feature Badges / Tech Stack chips)
- [ ] High contrast, crisp rendering with zero compression artifacts

---

## 📦 Phase 3: Standalone Packaging & Installer Compilation

### 3.1 Self-Contained Publishing
Configure self-contained single-file publishing so end-users need zero prerequisites:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o "./publish"
```

### 3.2 Inno Setup Script (`installer.iss`) Template
```iss
#define MyAppName "{{APP_NAME}}"
#define MyAppVersion "{{APP_VERSION}}"
#define MyAppPublisher "{{AUTHOR_NAME}}"
#define MyAppURL "https://github.com/{{GITHUB_USER}}/{{REPO_NAME}}"
#define MyAppExeName "{{EXE_NAME}}.exe"

[Setup]
AppId={{#MyAppName}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=dist-installer
OutputBaseFilename={#MyAppName}-Setup
SetupIconFile=Assets\app.ico
WizardSmallImageFile=Assets\wizard-small.bmp
WizardImageFile=Assets\wizard-large.bmp
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Start {#MyAppName} automatically on Windows logon"; GroupDescription: "Startup Options:"

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
```

### 3.3 1-Click Automated Build Script (`build-release.ps1`)
```powershell
$ErrorActionPreference = "Stop"
Write-Host ">>> 1. Generating Icons & Visual Assets..." -ForegroundColor Cyan
dotnet run -- --generate-icons

Write-Host ">>> 2. Publishing Single-File Executable..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "./publish"

Write-Host ">>> 3. Compiling Inno Setup Installer..." -ForegroundColor Cyan
$iscc = "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "ISCC.exe" }
& $iscc installer.iss

Write-Host ">>> SUCCESS! Setup ready in dist-installer/" -ForegroundColor Green
```

---

## ⚙️ Phase 4: CI/CD Automation & GitHub Workflows

Create `.github/workflows/build-release.yml`:
```yaml
name: Build & Release

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  build-and-release:
    runs-on: windows-latest
    permissions:
      contents: write

    steps:
      - name: Checkout Source
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup Inno Setup
        run: choco install innosetup --no-progress -y

      - name: Generate Icon Assets
        run: dotnet run -- --generate-icons

      - name: Publish Standalone Binary
        run: |
          dotnet publish -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -o "./publish"

      - name: Build Installer
        run: ISCC.exe installer.iss

      - name: Compute SHA256 Hashes
        id: hashes
        shell: pwsh
        run: |
          $setupHash = (Get-FileHash "dist-installer/*-Setup.exe" -Algorithm SHA256).Hash
          $exeHash = (Get-FileHash "publish/*.exe" -Algorithm SHA256).Hash
          echo "SETUP_HASH=$setupHash" >> $env:GITHUB_ENV
          echo "EXE_HASH=$exeHash" >> $env:GITHUB_ENV

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        if: startsWith(github.ref, 'refs/tags/')
        with:
          name: Release ${{ github.ref_name }}
          draft: false
          prerelease: false
          generate_release_notes: true
          files: |
            dist-installer/*-Setup.exe
            publish/*.exe
```

---

## 🏛️ Phase 5: Community Governance & Issue/PR Infrastructure

### 5.1 GitHub Issue Templates (`.github/ISSUE_TEMPLATE/`)
- [ ] `bug_report.yml`: Structured form with OS version, reproduction steps, expected vs actual behavior, and logs.
- [ ] `feature_request.yml`: Problem statement, proposed solution, and alternative considerations.
- [ ] `config.yml`: Link to GitHub Discussions and contact info for security/questions.

### 5.2 Pull Request Template (`.github/PULL_REQUEST_TEMPLATE.md`)
- [ ] Summary of changes
- [ ] Related Issue (`Fixes #...`)
- [ ] Type of change (Bug fix, new feature, breaking change, docs)
- [ ] Testing checklist verified

### 5.3 Community Documentation
- [ ] `CONTRIBUTING.md`: Dev environment setup, branch conventions, PR submission guidelines, and code style.
- [ ] `CODE_OF_CONDUCT.md`: Contributor Covenant v2.1 with enforcement contacts.
- [ ] `SECURITY.md`: Vulnerability reporting process and supported version matrix.

---

## 🏪 Phase 6: Package Manager Distribution Manifests

### 6.1 Windows Package Manager (WinGet)
Path: `manifests/winget/{{PUBLISHER}}.{{APP_NAME}}.yaml`
```yaml
PackageIdentifier: {{PUBLISHER}}.{{APP_NAME}}
PackageVersion: 1.0.0
PackageName: {{APP_NAME}}
Publisher: {{AUTHOR_NAME}}
License: MIT
Installers:
  - Architecture: x64
    InstallerType: inno
    InstallerUrl: https://github.com/{{GITHUB_USER}}/{{REPO_NAME}}/releases/download/v1.0.0/{{APP_NAME}}-Setup.exe
    InstallerSha256: 0000000000000000000000000000000000000000000000000000000000000000
    Scope: user
ManifestType: singleton
ManifestVersion: 1.6.0
```

### 6.2 Scoop Manifest
Path: `manifests/scoop/{{REPO_NAME}}.json`
```json
{
    "version": "1.0.0",
    "description": "{{SHORT_DESCRIPTION}}",
    "homepage": "https://github.com/{{GITHUB_USER}}/{{REPO_NAME}}",
    "license": "MIT",
    "architecture": {
        "64bit": {
            "url": "https://github.com/{{GITHUB_USER}}/{{REPO_NAME}}/releases/download/v1.0.0/{{APP_NAME}}-Setup.exe#/dl.7z",
            "hash": "0000000000000000000000000000000000000000000000000000000000000000"
        }
    },
    "bin": "{{EXE_NAME}}.exe",
    "shortcuts": [
        [
            "{{EXE_NAME}}.exe",
            "{{APP_NAME}}"
        ]
    ],
    "checkver": "github",
    "autoupdate": {
        "architecture": {
            "64bit": {
                "url": "https://github.com/{{GITHUB_USER}}/{{REPO_NAME}}/releases/download/v$version/{{APP_NAME}}-Setup.exe#/dl.7z"
            }
        }
    }
}
```

---

## 🌟 Phase 7: World-Class README & Showcase

### Required Structure:
1. **Title & Tagline**: Bold, clear, and memorable.
2. **Hero Banner**: High-resolution showcase graphic (`Assets/banner.png`).
3. **Shields Badges**: CI Build status, Latest Release, Framework/Language, License, Stars.
4. **Quick Install**: 1-line installation for WinGet and Scoop.
5. **Feature Showcase**: Bulleted breakdown of core capabilities with emojis and descriptions.
6. **Manual Downloads**: Direct installer & portable binary options.
7. **Building from Source**: Prerequisites + 1-click build instructions.
8. **Interactions & Controls**: Table mapping user gestures/keys to application behavior.
9. **Community & Contributing**: Links to `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and `SECURITY.md`.
10. **License**: Clear MIT / open-source disclaimer.

---

## 🎯 Phase 8: Tagging & Release Execution Checklist

When everything is verified and ready to ship:

```powershell
# 1. Review status and uncommitted files
git status

# 2. Stage all production files
git add .

# 3. Commit with semantic conventional commit
git commit -m "feat: complete production release setup with CI/CD and distribution manifests"

# 4. Push to main branch
git push origin main

# 5. Create and push official release tag (triggers CI/CD workflow)
git tag -a v1.0.0 -m "Release v1.0.0 - Initial Production Release"
git push origin v1.0.0
```

---
*Created with ❤️ for high-impact open-source software delivery.*
