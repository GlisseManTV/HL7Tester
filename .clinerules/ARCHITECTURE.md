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
│   ├── Services/             # App-level UI/platform services (file import, drag/drop)
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

Module for inspecting raw HL7 v2 messages with a hierarchical expandable tree:

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
- **Click-to-Copy** (v2.0.15):
  - `ToggleAndCopyCommand` — toggles expand/collapse AND copies node value to clipboard
  - `OnToggle` callback — allows ViewModel to wire the command to its `ToggleNode()` method
  - Arrow Label uses `ToggleCommand` only (expand/collapse)
  - Main Grid uses `ToggleAndCopyCommand` (expand/collapse + copy)

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
- HL7 documentation links (ADT, ORM, SIU) — opens URLs via `Launcher.Default.OpenAsync()`

### 7. Hl7FileImportService (Drag & Drop File Import)
**Location**: `HL7Tester/Services/Hl7FileImportService.cs`

App-level service for importing dropped HL7 text files into UI fields:
- Supports `.hl7`, `.h7`, `.txt`, `.msg`, `.dat`, `.edi`, `.log`
- Enforces a 2 MB maximum file size and rejects empty files
- Reads text as UTF-8 with BOM detection and normalizes line endings to `Environment.NewLine`
- Imports only the first file when multiple files are dropped
- Windows implementation uses `PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync()` for `StorageFile` access
- MacCatalyst implementation uses `PlatformArgs.DropSession.Items`, `NSItemProvider.LoadFileRepresentationAsync()`, and `LoadDataRepresentationAsync()` fallback
- Used by `MainPage.xaml.cs` to populate `MainViewModel.GeneratedMessage`
- Used by `Hl7InspectorPage.xaml.cs` to populate `Hl7InspectorViewModel.RawMessage` and automatically parse after import

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
<ApplicationDisplayVersion>2.0.16</ApplicationDisplayVersion>
<ApplicationVersion>2.0.16.0</ApplicationVersion>
```

### Windows Package Manifest
```xml
<!-- app.manifest -->
<assemblyIdentity version="2.0.16.0" name="HL7Tester.WinUI.app"/>

<!-- Package.appxmanifest -->
<Identity Name="ItConsult4Care.Hl7Tester" Publisher="..." Version="2.0.16.0" />
```

---

## What's New Page — Content Guidelines

### Purpose
The `WhatsNewPage` is an **end-user announcement page**, not a developer changelog. It is shown automatically when the user updates to a new version. Content must be warm, benefit-oriented, and immediately understandable by non-technical users.

### whatsnew.md Format

The file lives at `HL7Tester/Resources/Raw/whatsnew.md` and is embedded as a `MauiAsset`. Each `###` section becomes a **visual feature card** in the app.

**Required structure:**

```markdown
## vX.Y.Z

### 📂 Short User-Facing Title
> One sentence describing the benefit to the user. Not technical.
- Bullet point 1 (what the user can do)
- Bullet point 2
- Bullet point 3 (max 4 bullets recommended)

### 🔍 Another Feature Title
> Tagline for this feature.
- Bullet 1
```

**Rules:**

| Rule | Requirement |
|------|-------------|
| `## vX.Y.Z` | Exactly one version header per file (ignored visually) |
| `### Emoji Title` | Each section starts with an emoji, followed by a short title |
| `> Tagline` | Exactly one blockquote line per section — the key benefit statement |
| `- Bullets` | Optional. Max 4 bullets. Written in plain language, user benefit focus |
| Language | English only. Simple words. No jargon, no class names, no file paths |
| Technical details | **Never included** — strip all implementation details before publishing |

**Good example:**
```markdown
### 📂 Drop HL7 Files Directly into the App
> No more copy-paste. Just drag a file and you're done.
- Works on both Windows and macOS
- Supported formats: .hl7, .txt, .msg, .dat and more
- Dropped files are auto-parsed in the Inspector
```

**Bad example (do not do this):**
```markdown
### Cross-Platform HL7 File Drag & Drop
Added explicit drag-and-drop support for HL7 text files on both Windows and macOS MacCatalyst.
- Windows uses PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync()
- MacCatalyst uses NSItemProvider.LoadFileRepresentationAsync()
```

### How the WhatsNewPage Renders the Content

- Each `###` section → one `Border` card (rounded corners, subtle shadow)
- Emoji → large label on the left of the card
- Title → bold, `MidnightBlue` (light) / white (dark), font size 17
- Tagline → gray subtitle, font size 14
- Bullets → smaller gray text with `•` prefix, font size 13
- The `## vX.Y.Z` header is **not rendered** as a card (skipped by parser)
- `---` separators are also skipped

### When to Update whatsnew.md

Update `whatsnew.md` whenever a new version is released **before** bumping the version number in `.csproj`. The parser reads only the first `## vX.Y.Z` section, so older version blocks should be removed or kept out of the file.

---

## Recent Changes (v2.0.16)

### What's New Page — Announcement-Style Redesign

Replaced the flat markdown text block with a visually engaging, end-user-facing announcement layout. The page now presents each feature as a styled card, using warm benefit-oriented language instead of technical release notes.

**Key Features:**
- Each `###` section in `whatsnew.md` renders as a standalone `Border` card with rounded corners and a subtle shadow
- Card layout: large emoji on the left, bold title + gray tagline + bullet list on the right
- Fully respects light/dark theme (`AppTheme.Dark` check at card creation time)
- Header now includes a 🎉 emoji above the "What's New" title for a warm welcome feel
- "Got it!" button replaces the previous "OK, Got It!" label (no emoji in button text)

**Technical Details:**
- Removed `ContentLabel` (single `Label` with `FormattedText`) from XAML
- Added `FeatureCardsContainer` (`VerticalStackLayout`, `x:Name`) inside the `ScrollView`
- New `BuildAnnouncementCards(string markdown)` static method: parses `## / ### / > / -` lines into structured data, calls `CreateFeatureCard()` per section
- New `CreateFeatureCard(emoji, title, tagline, bullets)` static method: builds `Border > Grid > [emojiLabel | VerticalStackLayout]` entirely in code
- Font sizes: emoji 34px · title 20px · tagline 16px · bullets 15px
- `whatsnew.md` reformatted to follow the end-user announcement format (emoji titles, `>` taglines, benefit bullets — no technical details)

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/WhatsNewPage.xaml` | Replaced `ContentLabel` with `FeatureCardsContainer`; added 🎉 emoji to header; changed button text to "Got it!" |
| `HL7Tester/WhatsNewPage.xaml.cs` | Replaced `BuildFormattedReleaseNotes` with `BuildAnnouncementCards` + `CreateFeatureCard`; removed `System.Text.RegularExpressions` import |
| `HL7Tester/Resources/Raw/whatsnew.md` | Reformatted content in announcement style: emoji titles, `>` taglines, benefit-focused bullets, no technical details |
| `.clinerules/ARCHITECTURE.md` | Added "What's New Page — Content Guidelines" section documenting the `whatsnew.md` format and rendering rules |

---

### Cross-Platform HL7 File Drag & Drop

Added explicit drag-and-drop support for HL7 text files on both Windows and macOS MacCatalyst.

**Key Features:**
- Drop supported files into the `GeneratedMessage` area on MainPage to replace the current generated message
- Drop supported files into the HL7 Inspector raw message editor to import and automatically parse the message
- Supported extensions: `.hl7`, `.h7`, `.txt`, `.msg`, `.dat`, `.edi`, `.log`
- Only the first dropped file is imported when multiple files are dropped
- Empty files and files larger than 2 MB are rejected with a clear message

**Technical Details:**
- `Hl7FileImportService` centralizes file extension validation, size checks, UTF-8/BOM text reading, and line-ending normalization
- Windows file drops use native `StorageFile` extraction through `PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync()`
- MacCatalyst file drops use native `DropSession.Items` + `NSItemProvider` file/data representation APIs
- `DropGestureRecognizer` is attached explicitly to both target editor containers to avoid relying on platform-specific default editor behavior

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/Services/Hl7FileImportService.cs` | NEW — Cross-platform HL7 text file import service for dropped files |
| `HL7Tester/MainPage.xaml` | Added drop gesture to GeneratedMessage container; updated placeholder |
| `HL7Tester/MainPage.xaml.cs` | Added handler to import dropped file content into `GeneratedMessage` |
| `HL7Tester/Hl7InspectorPage.xaml` | Added drop gesture to raw message container; updated placeholder |
| `HL7Tester/Hl7InspectorPage.xaml.cs` | Added handler to import dropped file content and auto-parse it |
| `HL7Tester.csproj` | Version incremented to 2.0.16 |
| `Platforms/Windows/app.manifest` | Version incremented to 2.0.16.0 |
| `Platforms/Windows/Package.appxmanifest` | Version incremented to 2.0.16.0 |

---

## Recent Changes (v2.0.15)

### Main Page — Inspect Button Visibility & Layout

1. **Conditional "Inspect" Button Display**
   - The "Inspect" button in the footer is now hidden when `GeneratedMessage` is empty or null
   - Uses `StringNotEmptyConverter` to bind visibility to the message content
   - Only appears after a message has been generated (via "Generate HL7" button)

2. **Copy/Inspect Buttons Stacked Vertically**
   - Replaced horizontal side-by-side layout with a vertical stack inside the footer editor area
   - Both buttons share exactly 50% of the GeneratedMessage field height each (`RowDefinition Height="*"`)
   - `VerticalOptions="Fill"` ensures full height utilization
   - Added `RowSpacing="6"` for visual separation between buttons
   - Preserves application width (no horizontal expansion)

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/MainPage.xaml` | Added `StringNotEmptyConverter` to resources; replaced horizontal button layout with vertical stack in footer Grid using `RowDefinitions="*,*"` and `RowSpacing="6"`; added `IsVisible="{Binding GeneratedMessage, Converter={StaticResource StringNotEmptyConverter}}"` on Inspect button |

### Network Settings Page — Footer Alignment

1. **Footer Padding Aligned with HL7 Inspector**
   - Changed footer padding from `20,12` to `16,10` to match Hl7InspectorPage
   - Ensures consistent visual spacing for the Home button relative to bottom and right edges

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/NetworkSettingsPage.xaml` | Updated footer Grid padding from `20,12` to `16,10` to align with Hl7InspectorPage footer |

---

### HL7 Inspector — Click-to-Copy on Tree Nodes

Added click-to-copy functionality for values in the HL7 Inspector tree view, allowing users to quickly copy field values and notations to the clipboard.

**Key Features:**
- **Click on row (not arrow)**: Toggles expand/collapse AND copies the node's value to clipboard
- **Click on arrow (▼/▶)**: Toggles expand/collapse only
- Both actions use the same `ToggleNode()` method from ViewModel, ensuring proper tree manipulation
- Clipboard operations use `Clipboard.Default.SetTextAsync()` from MAUI

**Technical Details:**
- Added `ToggleAndCopyCommand` property to `Hl7TreeNode` that calls the ViewModel's `ToggleNode()` method then copies to clipboard
- Commands wired in `BuildSegmentNode()`, `BuildFieldNode()`, and component node creation
- XAML gesture recognizer on main Grid uses `ToggleAndCopyCommand`; arrow Label uses `ToggleCommand`

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/ViewModels/Hl7TreeNode.cs` | Added `ToggleAndCopyCommand` property; added `OnToggle` callback for ViewModel integration |
| `HL7Tester/ViewModels/Hl7InspectorViewModel.cs` | Assigned `ToggleAndCopyCommand` in `BuildSegmentNode()`, `BuildFieldNode()`, and component node creation — all using the shared `ToggleNode()` method |
| `HL7Tester/Hl7InspectorPage.xaml` | Updated tree row gesture recognizer to use `ToggleAndCopyCommand` on main Grid; arrow Label retains `ToggleCommand` for expand-only behavior |

### Send Parsed Message to Generated Message

Added a "Send Parsed →" button next to "Parse & Inspect" on the HL7 Inspector page, allowing users to send the parsed raw message directly into the `GeneratedMessage` field of the MainPage.

**Key Features:**
- **Button layout**: Grid with `*,Auto` columns — "Parse and Inspect" fills available space (master button), "Send Parsed →" is fixed 120×36px on the right
- **Static message bridge**: Uses `Hl7InspectorViewModel.PendingParsedMessage` static property to pass the raw HL7 text between ViewModels
- **MainPage picks up in `OnAppearing`**: Checks `PendingParsedMessage` and assigns it to `ViewModel.GeneratedMessage`, then clears the pending value

**Technical Details:**
- `Hl7InspectorPage.xaml.cs` — `OnSendParsedClicked` stores raw message in `PendingParsedMessage` and navigates via `Shell.Current.GoToAsync("//MainPage")`
- `Hl7InspectorViewModel.cs` — Added `public static string? PendingParsedMessage { get; set; }`
- `MainPage.xaml.cs` — `OnAppearing()` checks `PendingParsedMessage`, assigns to `GeneratedMessage`, clears the bridge
- `OnHomeClicked` in inspector also clears the pending message to prevent stale data

**Modified Files:**
| File | Changes |
|------|---------|
| `HL7Tester/Hl7InspectorPage.xaml` | Changed button row from `VerticalStackLayout` to `Grid` (`*,Auto`); added "Send Parsed →" button with fixed size and secondary style |
| `HL7Tester/Hl7InspectorPage.xaml.cs` | Added `OnSendParsedClicked` handler; updated `OnHomeClicked` to clear pending message |
| `HL7Tester/ViewModels/Hl7InspectorViewModel.cs` | Added `PendingParsedMessage` static property for cross-ViewModel communication |
| `HL7Tester/MainPage.xaml.cs` | Added `OnAppearing()` override to pick up pending messages from the inspector |

---

## Recent Changes (v2.0.14)

### Settings Page — Redesigned Layout
- **Three-row layout**: Network Settings + Connection History side-by-side on top, Application Settings full-width in the middle, HL7 Documentation + HL7 Tools side-by-bottom
- **HorizontalStackLayout → Grid** for button rows: buttons now fill the full width of their card with equal widths
  - HL7 Documentation card: 3 columns (`*,*,*`) for ADT, ORM, SIU buttons
  - HL7 Tools card: 2 columns (`*,*`) for Embedded Inspector and Web Inspector buttons
- All buttons within a single card have identical sizes

**Modified Files:**
| File | Changes |
|------|---------|
| `NetworkSettingsPage.xaml` | Redesigned layout with 3-row structure; replaced HorizontalStackLayout with Grid for uniform button sizing in Documentation and Tools cards |

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

*Last Updated: May 5, 2026 (v2.0.16)*
