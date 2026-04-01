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

## v2.0.8 Changes (Latest Release)

### MSH Segment Fields Shortening with Username

1. **Reduced MSH Routing Fields Length**
   - `SendingApplication`: Changed from `"Hl7Tester-Core"` to `"HL7Tester"`
   - `SendingFacility`: Changed from empty string to `Environment.UserName` for user identification
   - `ReceivingApplication`: Set to empty string `""` (was `"Receiver"`)
   - `ReceivingFacility`: Set to empty string `""` (was `"ReceiverFacility"`)

**Example before:**
```
MSH|^~\&|Hl7Tester-Core|HL7Tester|Receiver|ReceiverFacility|...
```

**Example after:**
```
MSH|^~\&|HL7Tester|Username|||20260401133300||ADT^A01|...
```

This reduces the MSH segment from 43 characters to approximately 20-25 characters, significantly shortening the generated HL7 message.

**Modified Files:**

| File | Changes |
|------|---------|
| `HL7Tester.Core/Class1.cs` | Updated default values for `SendingApplication`, `SendingFacility`, `ReceivingApplication`, and `ReceivingFacility` in `AdtMessageRequest` class |
| `HL7Tester.csproj` | Incremented version to `2.0.8` |
| `Platforms/Windows/app.manifest` | Incremented version to `2.0.8.0` |
| `Platforms/Windows/Package.appxmanifest` | Incremented version to `2.0.8.0` |

---

## v2.0.7 Changes (Latest release)

### ORM order control code sanitization

1. **Order Control Value Normalization**
   - ORC-1 now receives only the order control code (e.g., `NW`, `CA`, `XO`)
   - UI remains user-friendly with descriptive labels (e.g., "NW - New order")
   - Prevents full label text from being injected into HL7 ORM messages

**Modified Files:**

| File | Changes |
|------|---------|
| `ViewModels/MainViewModel.cs` | Extracts order control code before generating ORM messages |

---

## v2.0.6 Changes

### Collapsible Send Log Card with Dynamic Height

1. **Collapsed by Default**
   - Send log card is collapsed (reduced) by default to maximize form space
   - User can toggle visibility via click on the title header
   - Arrow indicator shows state: "► Send log" when collapsed, "▼ Send log" when expanded

2. **Arrow Indicator with BoolToArrowConverter**
   - Added new converter `Resources/Converters/BoolToArrowConverter.cs`
   - Converts boolean property to arrow indicator text
   - Binds to title label in XAML for visual feedback

3. **Dynamic Height Behavior**
   - Editor has auto-size behavior with maximum height constraint
   - Displays up to 5 lines of log messages (configurable via MaxLogLines constant)
   - Most recent logs appear at the top (newest first)
   - Auto-expands when a new log message is added if previously collapsed

4. **Toggle via Header Click**
   - Clicking on the Send log header toggles between expanded/collapsed states
   - Uses Tapped gesture recognizer on Grid header
   - Maintains scroll position during toggle

5. **Auto-Scroll to Recent Logs**
   - When a new log is appended, it appears at the top
   - Editor shows up to 5 most recent entries
   - Older entries are automatically discarded when limit is exceeded

**Example Log Output:**
```
[23:37:42] Message SEND ADT^A01|5fc50898e7633c4271b to 192.168.1.10:70. -> ACK
[23:35:12] Message SEND ORC^O01|ABC123 to 192.168.1.10:70. -> NACK
[23:30:45] Message SEND SIU^S12|XYZ789 to 192.168.1.10:70. -> No ACK
```

### Modified Files

| File | Changes |
|------|---------|
| `HL7Tester.csproj` | Application display version incremented to `2.0.5` |
| `MainPage.xaml` | Added BoolToArrowConverter resource; restructured Send log card with collapsible layout; added Tapped gesture on header; bound IsSendLogExpanded and SendLogHeightRequest properties |
| `MainPage.xaml.cs` | Added `OnSendLogHeaderTapped()` handler for toggle functionality |
| `ViewModels/MainViewModel.cs` | Added `IsSendLogExpanded` (bool), `SendLogHeightRequest` (double) properties; added `ToggleSendLog()` method; modified `AppendToSendLog()` to keep only 5 most recent lines and auto-expand on new entry |
| `Resources/Converters/BoolToArrowConverter.cs` | New file - IValueConverter that converts boolean to arrow indicator (►/▼) |

---

## v2.0.5 Changes

### UI Persistence - Generated Message Footer

1. **Pinned Generated Message Location**
   - Moved "Generated message" editor outside the ScrollView into a persistent footer
   - Footer positioned above the bottom action bar (Generate/Send/Settings buttons)
   - Grid layout: `RowDefinitions="Auto,*,Auto,Auto"` with footer in Row 3

2. **Dynamic AutoSize Behavior**
   - Removed fixed height from Generated message Editor
   - Using `AutoSize="TextChanges"` to expand automatically as HL7 segments are added
   - Each HL7 segment appears on a new line and the editor grows accordingly
   - Added `MaxHeight` constraint for scrollability control

3. **Footer Structure**
   - Row 0: Generated message editor + Copy button
   - Row 1: Action buttons (Generate HL7 / Send HL7 / Settings)
   - Footer persists at bottom while main form scrolls in Row 1

4. **Send Log Card Location**
   - Send log card remains inside the ScrollView in Row 1
   - Positioned after all form sections (Patient identity, Event/location, OBX, ORM, SIU)

**Example Generated Message:**
```
MSH|^~\&|HL7Tester|TEST|ADT|A01|20240324|SECURITY|ADT^A01|MSG00001|P|2.3
PID|1||PAT001^^^MRN|...
```

**Modified Files:**

| File | Changes |
|------|---------|
| `MainPage.xaml` | Reorganized Grid layout with 4 rows; moved Generated message to footer (Grid.Row="3"); removed fixed height, added AutoSize="TextChanges" |
| `MainPage.xaml.cs` | Added OnCopyMessageClicked event handler for clipboard copy |

---

## v2.0.6 Changes

### Send Log Card - Compact UI Mode

1. **Log Card Visibility Toggle**
   - Added option to collapse/hide the "Send log" card for reduced visual impact
   - Button in header toggles visibility: "🔽 Show Logs" / "🔼 Hide Logs"
   - Preserves scroll position when toggling

2. **Compact Footer Layout**
   - Generated message editor positioned immediately above action buttons
   - More vertical space available for form sections when logs are hidden
   - AutoSize behavior maintained for generated message expansion

3. **Smooth Transitions**
   - Visibility changes use conditional binding: `IsVisible="{Binding IsLogCardVisible}"`
   - Send log content persists while card is hidden
   - Re-expands to previous state when shown again

4. **ViewModel Integration**
   - Added `IsLogCardVisible` property in MainViewModel with change notification
   - Default value: `true` (logs visible)
   - Toggled via command binding for consistency with MVVM pattern

**Modified Files:**

| File | Changes |
|------|---------|
| `MainPage.xaml` | Added visibility toggle button; bind to `IsLogCardVisible` property |
| `ViewModels/MainViewModel.cs` | Added `IsLogCardVisible` property with getter/setter and PropertyChanged notification |

---

## Next Steps for Development

1. **Maintain English-Only Policy**: All new code, comments, and UI text must be in English
2. **Update Version**: Increment `<ApplicationDisplayVersion>` when preparing releases
3. **Log Directory**: Logs are stored in `~/.HL7Tester/` (macOS) or `%USER%\.HL7Tester\` (Windows)
4. **Daily Log Rotation**: One file per day (`yyyyMMdd.log`)

---

## v2.0.4 Changes

### Enhanced UI Logging

1. **Improved Send Log Format in Main View**
   - Changed from generic "[Send] Message sent to IP:PORT." to detailed format with timestamp, message type, control ID, and ACK status
   - New log format: `[HH:mm:ss] Message SEND ADT^A01|ControlID to IP:PORT. -> ACK`
   - Message Code (MSH-9) extraction from HL7 messages for display in UI logs
   - ACK/NACK status indicator appended to send confirmation

2. **Message Code Extraction**
   - Added `ExtractMessageCode()` method in `Hl7NetworkSender.cs`
   - Parses MSH segment field 9 (MessageType^SubType format includes Control ID)
   - Returns combined value like `ADT^A01|5fc50898e7633c4271b`

3. **SendResult Class**
   - Added to `Hl7NetworkSender.cs` to return detailed send results
   - Properties: `Success`, `MessageCode`, `AckMessage`, `ErrorMessage`
   - Replaces void return type for better information flow to UI

4. **UI Log Display in MainViewModel**
   - Updated `OnSendAsync()` method to format logs with timestamp and message details
   - Removed ACK content from UI (already logged to file)
   - Shows only status indicator: `ACK`, `NACK`, or `No ACK`

**Example Output:**
```
[14:59:49] Message SEND ADT^A01|5fc50898e7633c4271b to 192.168.1.10:70. -> ACK
```

### Modified Files

| File | Changes |
|------|---------|
| `HL7Tester.Core/Hl7NetworkSender.cs` | Added `SendResult` class; changed `SendAsync()` return type to `Task<SendResult>`; added `ExtractMessageCode()` for MSH-9 parsing |
| `ViewModels/MainViewModel.cs` | Updated `OnSendAsync()` with enhanced log formatting using timestamp, message code, and ACK status |

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
<ApplicationDisplayVersion>2.0.7</ApplicationDisplayVersion>
<ApplicationVersion>2.0.7.0</ApplicationVersion>
```

---

*Last Updated: April 1, 2026 (v2.0.8)*
