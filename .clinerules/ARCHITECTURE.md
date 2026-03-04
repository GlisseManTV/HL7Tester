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

## v2.0.1 Changes (Latest Release)

### New Features
1. **Dynamic Version Display**
   - Version now extracted automatically from assembly metadata
   - Displays as `HL7Tester vX.Y.Z` in Network Settings page
   - Eliminates manual version string updates

2. **Enhanced Logging**
   - Smart multi-line formatting for HL7 messages, ACKs, and errors
   - Message content logged before sending for debugging
   - ACK cleanup (removes MLLP control characters)

3. **Copy Button**
   - Added "Copy" button in main view to copy generated HL7 message to clipboard
   - Cross-platform using `Clipboard.SetTextAsync()`

4. **Version Comparison Fix**
   - Normalized version comparison ignoring Revision number (4th digit)
   - Prevents false update alerts between Windows and macOS builds

### Modified Files
| File | Changes |
|------|---------|
| `UpdateChecker.cs` | Added `CompareVersionWithoutRevision()` method |
| `NetworkSettingsViewModel.cs` | Dynamic version extraction from assembly |
| `NetworkSettingsPage.xaml` | Bound to `AssemblyVersion` property |
| `Hl7NetworkSender.cs` | Enhanced log formatting for HL7/ACK messages |
| `FileLoggerProvider.cs` | Smart line break insertion in logs |
| `MainPage.xaml` / `.cs` | Added Copy button functionality |

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
<ApplicationDisplayVersion>2.0.1</ApplicationDisplayVersion>
<ApplicationVersion>0</ApplicationVersion>
```

---

## Next Steps for Development

1. **Maintain English-Only Policy**: All new code, comments, and UI text must be in English
2. **Update Version**: Increment `<ApplicationDisplayVersion>` when preparing releases
3. **Log Directory**: Logs are stored in `~/.HL7Tester/` (macOS) or `%USER%\.HL7Tester\` (Windows)
4. **Daily Log Rotation**: One file per day (`yyyyMMdd.log`)

---

*Last Updated: March 3, 2026*