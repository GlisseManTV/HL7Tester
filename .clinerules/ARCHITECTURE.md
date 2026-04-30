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
│   │   ├── Converters/       # IValueConverter implementations
│   │   └── Styles/           # Global styles (Colors.xaml, Styles.xaml)
│   ├── ViewModels/           # MVVM ViewModels
│   ├── Logging/              # Custom file logging implementation
│   └── *.xaml/.xaml.cs       # UI pages and code-behind
├── HL7Tester.Core/           # Shared business logic library
│   ├── Hl7NetworkSender.cs   # HL7 message sending with MLLP framing + encoding
│   ├── UpdateChecker.cs      # GitHub release update checking
│   ├── NetworkSettings.cs    # Settings data model (incl. MessageEncoding)
│   ├── Inspector/            # HL7 Message Inspector
│   │   ├── Models/           # ParsedHL7Message, HL7SegmentModel, etc.
│   │   └── Services/         # HL7ParserService, HL7KnowledgeBase, etc.
│   └── UpdateChecker.cs      # GitHub release update checking
├── HL7Tester.Tests/          # Unit tests
└── ARCHITECTURE.md           # This file
```

---

## Architecture Patterns

### MVVM (Model-View-ViewModel)
- **Views**: XAML pages (`MainPage.xaml`, `NetworkSettingsPage.xaml`, `Hl7InspectorPage.xaml`)
- **ViewModels**: `MainViewModel.cs`, `NetworkSettingsViewModel.cs`, `Hl7InspectorViewModel.cs`
- **Models**: `NetworkSettings.cs`, `Hl7TreeNode.cs`, `HL7SegmentModel.cs`, etc.

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
- **Custom message encoding support** — users can select from predefined encodings (UTF-8, Windows-1252, ASCII) or specify a custom encoding name
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
- Message encoding selection (predefined or custom)
- Log level preference
- Auto-update toggle
- Connection history

### 5. HL7 Inspector — Message Parsing & Tree View
**Location**: `HL7Tester.Core/Inspector/` + `HL7Tester/Hl7InspectorPage.xaml`

New module for inspecting raw HL7 v2 messages with a hierarchical expandable tree:

**Core Models** (`HL7Tester.Core/Inspector/Models/`):
- `ParsedHL7Message` — top-level parsed result (version, segments, parse error)
- `HL7SegmentModel` — segment with Id, Name, Fields list
- `HL7FieldModel` — field with Index, DataType, Required, Repetitions
- `HL7RepetitionModel` — repetition within a field (separated by `~`)
- `HL7ComponentModel` — component within a repetition (separated by `^`)
- `HL7SubComponentModel` — sub-component (separated by `&`)

**Services** (`HL7Tester.Core/Inspector/Services/`):
- `HL7ParserService` — main parser entry point
  - Reads encoding chars dynamically from MSH-1..5
  - Handles MLLP framing stripping (`\u000b`, `\u001c`)
  - Special MSH handling: synthetic MSH-1 = `|`, field index offset by +1
  - HL7 escape sequence decoding (`\F\`, `\S\`, `\R\`, `\E\`, `\T\`)
  - Field repetition parsing (`~`), component parsing (`^`), sub-component parsing (`&`)
- `HL7KnowledgeBase` — comprehensive static dictionary:
  - 50+ segment descriptions (MSH, EVN, PID, PV1, OBX, ORC, OBR, etc.)
  - Field info keyed by `SEGMENT-FIELDINDEX` → (Name, DataType, Required)
  - Component names for 30+ data types (CWE, CX, XPN, XAD, PL, XCN, EI, HD, TS, etc.)
- `HL7VersionDetector` — detects HL7 version from MSH-12

**ViewModel** (`HL7Tester/ViewModels/Hl7TreeNode.cs` + `Hl7InspectorViewModel.cs`):
- `Hl7TreeNode` — tree node with Level enum (Segment=0, Field=1, Component=2, SubComponent=3)
- Flat `ObservableCollection<Hl7TreeNode>` for visible nodes only
- `ToggleNode()` inserts/removes children dynamically
- Indentation = Level × 18px, font size decreases per level
- `NodeLevelToColorConverter` — color by level (Segment=indigo, Field=near-black, Component=gray, SubComponent=lighter gray)

**UI** (`HL7Tester/Hl7InspectorPage.xaml`):
- Grid layout: `RowDefinitions="Auto,Auto,*,Auto"` (input, status, tree, footer)
- Editor for raw HL7 message input
- `CollectionView` with `DataTemplate x:DataType="Hl7TreeNode"` showing expandable flat tree
- DataType badge (e.g., `[PL]`, `[CWE]`) shown conditionally
- Static footer with Home button

### 6. NetworkSettingsViewModel
**Location**: `HL7Tester/ViewModels/NetworkSettingsViewModel.cs`

Manages network settings UI:
- IP address, port, nickname fields
- Connection history management (unique entries, no duplicates)
- Log level selection
- Auto-update toggle
- Message encoding selection (predefined + custom input)

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
  "MessageEncoding": "UTF-8",
  "LogLevel": "Debug",
  "AutoUpdateCheck": true,
  "History": [
    { "Ip": "192.168.1.10", "Port": "70", "Nickname": "Server A" }
  ]
}
```

### HL7Tester.csproj (Version Configuration)
```xml
<ApplicationDisplayVersion>2.0.13</ApplicationDisplayVersion>
<ApplicationVersion>2.0.13.0</ApplicationVersion>
```

### Windows Package Manifest
```xml
<!-- app.manifest -->
<assemblyIdentity version="2.0.13.0" name="HL7Tester.WinUI.app"/>

<!-- Package.appxmanifest -->
<Identity Name="ItConsult4Care.Hl7Tester" Publisher="..." Version="2.0.13.0" />
```

---

## Recent Changes (v2.0.13)

### HL7 Message Inspector — New Module
- **Complete hierarchical parsing**: Raw HL7 message → Segment → Field → Component → SubComponent tree
- **Dynamic encoding detection**: Reads MSH encoding characters (`MSH-1` through `MSH-5`) at parse time
- **Comprehensive knowledge base**: 50+ segment descriptions, all field names/types/required flags, 30+ data type component mappings
- **Flat expandable tree UI**: Only visible nodes stored in `ObservableCollection<Hl7TreeNode>`, children inserted on expand
- **Color-coded levels**: Indigo for segments, near-black for fields, gray for components, lighter gray for sub-components
- **DataType badges**: Shows data type (e.g., `[PL]`, `[CWE]`) next to field notation
- **HL7 escape sequence decoding**: `\F\` (field separator), `\S\` (sub-component), `\R\` (repetition), `\E\` (escape), `\T\` (text)
- **MLLP framing stripping**: Automatically removes `\u000b` and `\u001c` control characters
- **MSH special handling**: Synthetic MSH-1 = `|` (the field separator itself), field index offset by +1

**New Files:**
| File | Purpose |
|------|---------|
| `HL7Tester.Core/Inspector/Models/ParsedHL7Message.cs` | Top-level parsed result model |
| `HL7Tester.Core/Inspector/Models/HL7SegmentModel.cs` | Segment with fields list |
| `HL7Tester.Core/Inspector/Models/HL7FieldModel.cs` | Field with data type, required flag, repetitions |
| `HL7Tester.Core/Inspector/Models/HL7RepetitionModel.cs` | Repetition (separated by `~`) |
| `HL7Tester.Core/Inspector/Models/HL7ComponentModel.cs` | Component (separated by `^`) |
| `HL7Tester.Core/Inspector/Models/HL7SubComponentModel.cs` | Sub-component (separated by `&`) |
| `HL7Tester.Core/Inspector/Services/HL7ParserService.cs` | Main parser service |
| `HL7Tester.Core/Inspector/Services/HL7KnowledgeBase.cs` | Static dictionary of 50+ segments, fields, components |
| `HL7Tester.Core/Inspector/Services/HL7VersionDetector.cs` | Detects HL7 version from MSH-12 |
| `HL7Tester/ViewModels/Hl7TreeNode.cs` | Tree node model with expand/collapse logic |
| `HL7Tester/Resources/Converters/NodeLevelToColorConverter.cs` | Converts NodeLevel enum to display color |

**Modified Files:**
| File | Changes |
|------|---------|
| `Hl7InspectorPage.xaml` | Complete rewrite: Grid 4-row layout, flat expandable tree CollectionView |
| `Hl7InspectorViewModel.cs` | Rewritten with HL7ParserService injection, flat ObservableCollection<Hl7TreeNode> |
| `MauiProgram.cs` | Added DI registration for HL7ParserService, Hl7InspectorViewModel, Hl7InspectorPage |

### Message Encoding Support
- **Custom message encoding**: Users can select encoding (UTF-8, Windows-1252, ASCII) or specify custom name when sending messages
- `NetworkSettings.MessageEncoding` property added
- UI in NetworkSettingsPage with dropdown + custom input field
- `Hl7NetworkSender` uses selected encoding for byte conversion

---

*Last Updated: April 30, 2026 (v2.0.13)*