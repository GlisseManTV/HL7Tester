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

## v2.0.3 Changes (Latest Release)

### Message Families and Generation

1. **Expanded Message Family Support**
   - Added full UI selection flow for `ADT`, `ORM`, and `SIU` families
   - Added dynamic message type lists per selected family

2. **ORM/SIU Generation Support in Core**
   - Added `ORM O01` message generation path using segment-based textual generation
   - Added `SIU S12/S13/S14/S15` message generation path using segment-based textual generation
   - Kept ADT generation on NHapi object-based flow for compatibility

3. **Extended Automated Tests**
   - Added/updated unit tests to cover `ADT A01`, `ORM O01`, and `SIU S12/S13/S14/S15`
   - Adjusted assertions to remain robust while validating required segment content

### UI and UX Improvements

1. **Header Alignment Improvement**
   - `Message Family` and `Message Type` pickers are now aligned on the same row in the header

2. **ORM Section Grouping by Segment**
   - Reorganized non-ADT right panel to clearly group ORM fields into:
     - `ORC segment`
     - `OBR segment`
     - `DG1 segment`
   - Added explicit field-position hints in labels (e.g. `ORC-5`, `DG1-3.1`)

3. **SIU Section Grouping by Segment**
   - Reorganized SIU fields into clear segment blocks:
     - `SCH segment`
     - `AIG segment`
     - `AIL segment`
     - `AIP segment`
   - Added explicit field-position hints and note about `RGS` auto-generation

### Stability and Robustness Fixes

1. **UI Family Switch Crash Fix (ORM/SIU -> ADT)**
   - Hardened `SelectedMessageFamily` handling with fallback to `ADT`
   - Hardened `SelectedMessageType` handling with null-safe assignment
   - Made family/type refresh path null-safe and consistent during picker transitions
   - Prevented null-triggered failures in context update logic

### Validation Summary

- XAML compilation validated via MacCatalyst compile target
- Manual runtime validation confirmed no crash when switching between ADT/ORM/SIU and back to ADT

### Modified Files

| File | Changes |
|------|---------|
| `MainPage.xaml` | Header pickers aligned on one row; ORM/SIU forms reorganized by HL7 segment groups with clearer labels |
| `ViewModels/MainViewModel.cs` | Null-safe family/type transitions; ADT fallback; safer UI context updates |
| `HL7Tester.Core/Class1.cs` | Routing and generation support for ORM O01 and SIU S12/S13/S14/S15 (segment-based generation) |
| `HL7Tester.Tests/Test1.cs` | Added/updated tests for ORM/SIU plus ADT coverage alignment |
| `HL7Tester.csproj` | Application display version incremented to `2.0.3` |

### UI Improvements

1. **HL7 Documentation Links in Network Settings**
   - Added three clickable buttons in Network Settings page for quick access to HL7 v2.3 documentation
   - Buttons: "HL7 ADT v2.3 ↗", "HL7 ORM v2.3 ↗", "HL7 SIU v2.3 ↗"
   - Links point to official HL7 Europe documentation with specific section anchors:
     | Message Type | URL Section |
     |--------------|-------------|
     | ADT | CH3.html#Heading3 |
     | ORM | CH4.html#Heading13 |
     | SIU | CH10.html#Heading53 |
   - Implemented using `Launcher.Default.OpenAsync()` for cross-platform URL opening
   - Buttons arranged horizontally in the documentation card with equal width distribution

### Modified Files (Documentation Update)

| File | Changes |
|------|---------|
| `NetworkSettingsPage.xaml` | Added horizontal stack layout with three documentation buttons in HL7 Documentation card |
| `ViewModels/NetworkSettingsViewModel.cs` | Added `OpenDocumentationCommand`, `OpenOrmDocumentationCommand`, `OpenSiuDocumentationCommand` properties and handler methods with URL constants |

---

## v2.0.2 Changes

### UI Improvements

1. **Fixed EVN-6-1 Field Alignment**
   - Changed column 2 width from `"3*"` to `"400"` for consistent field sizing
   - Entry now occupies fixed 400px width while remaining aligned properly

2. **Enhanced Form Labels and Placeholders**
   - Moved HL7 segment references from labels to placeholders for cleaner UI
   - Applied to Patient identity, Patient location, and OBX segments sections

3. **Global Hover Effects (Performance-Friendly)**
   - Added `PointerOver` and `Pressed` visual states to all interactive controls via global styles
   - Uses `VisualStateManager` only — no code-behind event handlers → zero performance impact
   - Applied to:
     | Control | Hover Effect | Pressed Effect |
     |---------|--------------|----------------|
     | Button | Darker background, opacity 0.97, scale 1.02 | Tertiary color, opacity 0.92, scale 0.98 |
     | ImageButton | Opacity 0.9, scale 1.05, overlay tint | Opacity 0.8, scale 0.98 |
     | Entry/Editor | Light hover background (light) / dark hover (dark) | Same as hover + `Focused` state |
     | Picker | Light hover background | N/A |
     | Switch | Opacity 0.9 | N/A |
     | CheckBox/RadioButton | Opacity 0.9 / overlay background | N/A |
     | DatePicker/TimePicker | Light hover background | N/A |
     | SearchBar/SearchHandler | Light hover background | N/A |
     | Border (Cards) | Stroke color change + hover background | N/A |

   - Added `ClickableListItemBorder` style for list items with click feedback (e.g., connection history)
   - Fixed ZIndex layering to prevent Editor background from overlapping header buttons (Copy, Logs)

### Modified Files

| File | Changes |
|------|---------|
| `MainPage.xaml` | Fixed EVN-6-1 column width (`Width="400"`), removed `HorizontalOptions="End"` from Entry |
| `MainPage.xaml` | Moved HL7 references to placeholders: PID-5.1, PID-5.2, yyyyMMdd, PV1-3.1 through PV1-3.8, OBX-3-1, OBX-5-1 |
| `MainPage.xaml` | Added `ZIndex="0"` to Editors, `ZIndex="1"` to header Grid, `ZIndex="2"` to Copy/Logs buttons |
| `NetworkSettingsPage.xaml` | Changed history list items from `Grid` to `Border` with `ClickableListItemBorder` style |
| `Resources/Styles/Colors.xaml` | Added hover colors (`SurfaceHoverLight`, `SurfaceHoverDark`, `HoverOverlayLight`, `HoverOverlayDark`) and brushes |
| `Resources/Styles/Styles.xaml` | Enhanced Button, ImageButton, Entry, Editor, Picker, Switch, CheckBox, RadioButton, DatePicker, TimePicker, SearchBar, SearchHandler with PointerOver/Pressed/Focused states; added CardBorder hover feedback |

---

## v2.0.1 Changes

### New Features

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
<ApplicationDisplayVersion>2.0.3</ApplicationDisplayVersion>
<ApplicationVersion>0</ApplicationVersion>
```

---

## Next Steps for Development

1. **Maintain English-Only Policy**: All new code, comments, and UI text must be in English
2. **Update Version**: Increment `<ApplicationDisplayVersion>` when preparing releases
3. **Log Directory**: Logs are stored in `~/.HL7Tester/` (macOS) or `%USER%\.HL7Tester\` (Windows)
4. **Daily Log Rotation**: One file per day (`yyyyMMdd.log`)

---

*Last Updated: March 5, 2026 (v2.0.3)*