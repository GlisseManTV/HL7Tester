# HL7Tester v2 Architecture Documentation

## Overview

HL7Tester is a cross-platform .NET MAUI application for generating and sending HL7 v2.3 messages to HL7 servers. It supports Windows, macOS (MacCatalyst), iOS, and Android platforms.

---

## Project Structure

```
V2/
├── HL7Tester/                 # Main MAUI Application
│   ├── Platforms/            # Platform-specific implementations
│   ├── Resources/            # Assets, fonts, images, styles
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Logging/              # Custom file logging implementation
│   └── *.xaml/.xaml.cs       # UI pages and code-behind
├── HL7Tester.Core/           # Shared business logic library
│   ├── Hl7NetworkSender.cs   # HL7 message sending with MLLP framing
│   ├── UpdateChecker.cs      # GitHub release update checking
│   └── NetworkSettings.cs    # Settings data model
├── HL7Tester.Tests/          # Unit tests
└── ARCHITECTURE.md           # This file
```

---

## Architecture Patterns

### MVVM (Model-View-ViewModel)
- **Views**: XAML pages (`MainPage.xaml`, `NetworkSettingsPage.xaml`)
- **ViewModels**: `MainViewModel.cs`, `NetworkSettingsViewModel.cs`
- **Models**: `NetworkSettings.cs`

### Dependency Injection
- Services registered in `MauiProgram.cs`
- Constructor injection in ViewModels

---

## Core Components

### 1. FileLoggerProvider (Logging)
**Location**: `HL7Tester/Logging/FileLoggerProvider.cs`

Custom `ILoggerProvider` implementation that:
- Writes logs to `~/.HL7Tester/` directory (macOS) or `%USER%\.HL7Tester\` (Windows)
- Creates one log file per day named `yyyyMMdd.log` (e.g., `20260303.log`)
- Supports log levels: DEBUG, INFO, WARNING, ERROR
- Smart formatting for HL7 messages, ACK responses, and error messages with line breaks

**Configuration**: Read from `networksettings.json` at startup

### 2. Hl7NetworkSender (Network Communication)
**Location**: `HL7Tester.Core/Hl7NetworkSender.cs`

Handles HL7 message transmission:
- MLLP framing with `<VT>` and `<FS><CR>` delimiters
- Configurable timeout for ACK reception (3 seconds default)
- Structured logging of send attempts, results, and received ACKs

### 3. UpdateChecker (Version Management)
**Location**: `HL7Tester.Core/UpdateChecker.cs`

Checks for updates on GitHub releases:
- Compares versions using only Major.Minor.Build (ignores Revision number)
- Handles cross-platform version format differences (Windows: `2.0.1.0`, macOS: `2.0.1`)
- Provides download URL from release assets

### 4. NetworkSettingsService
**Location**: `HL7Tester.Core/NetworkSettings.cs`

Persists application settings to JSON:
- IP address and port configuration
- Log level preference
- Auto-update toggle
- Connection history

---

## Development Guidelines

### Language Policy: ENGLISH ONLY
**All code must use English exclusively:**
- **Labels and text strings**: Use English (e.g., "IP address", "Port", "Save configuration")
- **Placeholders**: Use English (e.g., "Enter IP address", "Enter port number")
- **Comments**: Write all code comments in English
- **Documentation**: All documentation files must be in English

This ensures consistency, maintainability, and accessibility for international developers.

### Code Quality Standards
1. **Async/Await Pattern**: Use `async/await` with `.ConfigureAwait(false)` for library code
2. **Null Safety**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
3. **ILogger Pattern**: Use dependency-injected `ILogger<T>` for all logging
4. **MVVM Separation**: Keep UI logic in Views, business logic in ViewModels

### Logging Best Practices
- **DEBUG**: Detailed diagnostic information (user info, device details, configuration)
- **INFO**: Important operational events (IP changes, message sends, updates checked)
- **WARNING**: Potential issues that don't stop execution
- **ERROR**: Errors that require attention (send failures, network errors)

---

## Configuration Files

### networksettings.json
```json
{
  "LastIpAddress": "192.168.1.10",
  "LastPort": "70",
  "LogLevel": "Debug",
  "AutoUpdateCheck": true,
  "History": [
    { "Ip": "192.168.1.10", "Port": "70" }
  ]
}
```

### HL7Tester.csproj (Version Configuration)
```xml
<ApplicationDisplayVersion>2.0.12</ApplicationDisplayVersion>
<ApplicationVersion>2.0.12.0</ApplicationVersion>
```

### Windows Package Manifest
```xml
<!-- app.manifest -->
<assemblyIdentity version="2.0.12.0" name="HL7Tester.WinUI.app"/>

<!-- Package.appxmanifest -->
<Identity Name="ItConsult4Care.Hl7Tester" Publisher="..." Version="2.0.12.0" />
```

---

## Recent Changes (v2.0.12)

### UI Improvements
- **Static Footer in Settings Page**: Redesigned `NetworkSettingsPage.xaml` with `Grid` layout (`RowDefinitions="*,Auto"`) - scrollable content in Row 0, fixed footer with Home button and version in Row 1
- **Connection History Text Size**: Fixed inconsistent font size by removing explicit `FontSize="Medium"` from history list items (now uses default 14)
- **Deprecated API Replacements**:
  - `DisplayAlert()` → `DisplayAlertAsync()` across all code-behind files
  - `Application.Current?.MainPage` → `Windows[0]?.Page` for main window access
  - `Frame` → `Border` for CardFrame style (Frame obsolete in .NET 9+)
  - `LayoutOptions.FillAndExpand` → `Fill` for button layout

---

*Last Updated: April 19, 2026 (v2.0.12)*
