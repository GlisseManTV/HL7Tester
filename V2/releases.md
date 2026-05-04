## v2.0.15 Changes (Latest Release)

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
| `HL7Tester/ViewModels/Hl7TreeNode.cs` | Added `ToggleAndCopyCommand` property and constructor initialization; added `OnToggle` callback for ViewModel integration |
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

## v2.0.14 Changes (Previous Release)

### Settings Page — Redesigned Layout

Complete restructure of the Settings page with a new three-row card layout and improved button sizing in documentation/tools cards.

**Key Changes:**
- **Three-row layout**: Network Settings + Connection History side-by-side on top, Application Settings full-width in the middle, HL7 Documentation + HL7 Tools side-by-bottom
- **HorizontalStackLayout → Grid** for button rows: buttons now fill the full width of their card with equal widths
  - HL7 Documentation card: 3 columns (`*,*,*`) for ADT, ORM, SIU buttons
  - HL7 Tools card: 2 columns (`*,*`) for Embedded Inspector and Web Inspector buttons
- All buttons within a single card have identical sizes

**Modified Files:**
| File | Changes |
|------|---------|
| `NetworkSettingsPage.xaml` | Redesigned layout with 3-row structure; replaced HorizontalStackLayout with Grid for uniform button sizing in Documentation and Tools cards |
| `HL7Tester.csproj` | Version incremented to 2.0.14 |

---

## v2.0.13 Changes (Previous Release)

### HL7 Message Inspector — New Module for Hierarchical Message Inspection

Complete rewrite of the HL7 Inspector page with a hierarchical expandable tree view (Segment → Field → Component → SubComponent) and comprehensive knowledge base for semantic descriptions.

**Key Features:**
- **Flat expandable tree UI**: Only visible nodes stored in `ObservableCollection<Hl7TreeNode>`, children dynamically inserted on expand
- **Dynamic encoding detection**: Reads MSH encoding characters (`MSH-1` through `MSH-5`) at parse time
- **Comprehensive knowledge base**: 50+ segment descriptions, all field names/types/required flags, 30+ data type component mappings
- **Color-coded levels**: Indigo for segments, near-black for fields, gray for components, lighter gray for sub-components
- **DataType badges**: Shows data type (e.g., `[PL]`, `[CWE]`) next to field notation
- **HL7 escape sequence decoding**: `\F\` (field separator), `\S\` (sub-component), `\R\` (repetition), `\E\` (escape), `\T\` (text)
- **MLLP framing stripping**: Automatically removes `\u000b` and `\u001c` control characters
- **MSH special handling**: Synthetic MSH-1 = `|` (the field separator itself), field index offset by +1

### Message Encoding Support for HL7 Transmission

Refactor of the HL7 network sending pipeline to support custom message encodings, allowing users to specify encoding when sending messages.

**Key Changes:**
- `NetworkSettings.MessageEncoding` property added (string, default "UTF-8")
- UI in `NetworkSettingsPage` with dropdown for predefined encodings (UTF-8, Windows-1252, ASCII) and custom input field
- `Hl7NetworkSender` uses selected encoding for byte conversion (via `Encoding.GetEncoding()` or `Encoding.GetEncodingProvider()`)
- `MainViewModel` passes selected encoding to the network sender
- `NetworkSettingsViewModel` handles selection and saving of message encoding settings
- Smart logging of encoding used for each send operation

### Modified Files:

| File | Changes |
|------|---------|
| `HL7Tester.Core/Inspector/Models/ParsedHL7Message.cs` | NEW — Top-level parsed result model |
| `HL7Tester.Core/Inspector/Models/HL7SegmentModel.cs` | NEW — Segment with fields list |
| `HL7Tester.Core/Inspector/Models/HL7FieldModel.cs` | NEW — Field with data type, required flag, repetitions |
| `HL7Tester.Core/Inspector/Models/HL7RepetitionModel.cs` | NEW — Repetition (separated by `~`) |
| `HL7Tester.Core/Inspector/Models/HL7ComponentModel.cs` | NEW — Component (separated by `^`) |
| `HL7Tester.Core/Inspector/Models/HL7SubComponentModel.cs` | NEW — Sub-component (separated by `&`) |
| `HL7Tester.Core/Inspector/Services/HL7ParserService.cs` | NEW — Main parser service with MLLP stripping, escape decoding, dynamic separators |
| `HL7Tester.Core/Inspector/Services/HL7KnowledgeBase.cs` | NEW — Static dictionary of 50+ segments, fields, components |
| `HL7Tester.Core/Inspector/Services/HL7VersionDetector.cs` | NEW — Detects HL7 version from MSH-12 |
| `HL7Tester/ViewModels/Hl7TreeNode.cs` | NEW — Tree node model with expand/collapse logic, indentation, font size per level |
| `HL7Tester/ViewModels/Hl7InspectorViewModel.cs` | REWRITTEN — Uses HL7ParserService injection, flat ObservableCollection<Hl7TreeNode>, ToggleNode/Expand/Collapse |
| `HL7Tester/Hl7InspectorPage.xaml` | REWRITTEN — Grid 4-row layout (Auto,Auto,*,Auto), flat expandable tree CollectionView, DataType badges |
| `HL7Tester/Resources/Converters/NodeLevelToColorConverter.cs` | NEW — Converts NodeLevel enum to display color |
| `HL7Tester/MauiProgram.cs` | Added DI registration for HL7ParserService, Hl7InspectorViewModel, Hl7InspectorPage |
| `HL7Tester.Core/NetworkSettings.cs` | Added `MessageEncoding` property with JSON persistence |
| `HL7Tester/NetworkSettingsPage.xaml` | Added message encoding dropdown + custom input field |
| `HL7Tester/ViewModels/NetworkSettingsViewModel.cs` | Added message encoding selection logic, save/load from settings |
| `HL7Tester.Core/Hl7NetworkSender.cs` | Uses selected encoding for byte conversion, logs encoding used |
| `HL7Tester/MainViewModel.cs` | Passes selected encoding to Hl7NetworkSender |
| `HL7Tester.csproj` | Version incremented to 2.0.13 |
| `Platforms/Windows/app.manifest` | Version incremented to 2.0.13.0 |
| `Platforms/Windows/Package.appxmanifest` | Version incremented to 2.0.13.0 |

---

## v2.0.12 Changes (Latest Development)

### Settings Page UI Improvements

1. **Static Footer with Home Button**
   - Redesigned Network Settings page layout using `Grid` with `RowDefinitions="*,Auto"`
   - Scrollable content area in Row 0 (ScrollView with all cards)
   - Static footer in Row 1 that stays visible at the bottom
   - Home button and app version displayed in the fixed footer
   - Footer background matches app theme (`White` for light, `OffBlack` for dark)

2. **Connection History Text Size Fix**
   - Removed explicit `FontSize="Medium"` from history list item Label
   - History entries now use the default font size (14) from the global Label style
   - Consistent text sizing across the entire settings page

3. **Deprecated API Replacements**
   - Replaced all `DisplayAlert()` calls with `await DisplayAlertAsync()` in `MainPage.xaml.cs` and `App.xaml.cs`
   - Replaced deprecated `Application.Current?.MainPage` with `Windows[0]?.Page` in `App.xaml.cs`
   - Converted obsolete `Frame` style to `Border` in `Styles.xaml` (CardFrame)
   - Replaced deprecated `LayoutOptions.FillAndExpand` with `Fill` in documentation buttons

**Modified Files:**

| File | Changes |
|------|---------|
| `NetworkSettingsPage.xaml` | Restructured with Grid layout (ScrollView + static footer); Moved Home button and version to footer; Fixed history item FontSize |
| `MainPage.xaml.cs` | Replaced `DisplayAlert` with `DisplayAlertAsync` (4 occurrences) |
| `App.xaml.cs` | Replaced `Application.Current?.MainPage.DisplayAlert` with `Windows[0]?.Page.DisplayAlertAsync`; Added null safety |
| `Resources/Styles/Styles.xaml` | Converted `Frame` style to `Border` for CardFrame; Updated property names (BackgroundColor→Background, BorderColor→Stroke) |
| `HL7Tester.csproj` | Version incremented to 2.0.12 |
| `Platforms/Windows/app.manifest` | Version incremented to 2.0.12.0 |
| `Platforms/Windows/Package.appxmanifest` | Version incremented to 2.0.12.0 |

---

## v2.0.11 Changes (Previous)

### Enhanced Nickname Handling & History Management

1. **Improved Nickname Parsing in History Selection**
   - Fixed bug where nickname content went into Port field when clicking history entry
   - Added proper parsing of "IP:Port (Nickname)" format in `OnSelectHistoryEntry()`
   - Uses string indexing to extract IP, Port, and Nickname separately

**Example:**
```
Entry: "192.168.1.10:70 (Server A)"
- IpAddress → "192.168.1.10"
- Port → "70"  
- Nickname → "Server A"
```

2. **Delete History Button**
   - Added "Delete History" button in Connection History card
   - Clears entire connection history with single click
   - Implemented via `OnDeleteHistory()` method and `DeleteHistoryCommand`

3. **Historique Refresh While Preserving Values**
   - Modified `SaveNetworkSettingsAsync()` to update HistoryEntries without clearing IP/Port/Nickname fields
   - Sets `_settings.Nickname` before saving to ensure persistence
   - Manually refreshes history collection after save instead of calling `LoadAsync()`

### Modified Files:

| File | Changes |
|------|---------|
| `ViewModels/NetworkSettingsViewModel.cs` | Enhanced `OnSelectHistoryEntry()` with proper nickname parsing; Added `OnDeleteHistory()` method and `DeleteHistoryCommand`; Modified `SaveNetworkSettingsAsync()` to preserve field values while updating history |

---

## v2.0.10 Changes (Latest Release)

### Nickname Support & Unique History Entries

1. **Nickname Field for IP Addresses**
   - Added optional nickname field to label IP addresses with friendly names
   - Stored in both `NetworkSettings.Nickname` and `ConnectionHistoryEntry.Nickname`
   - Displayed as `"IP:Port (Nickname)"` in connection history
   - Applied during save operation in `SaveNetworkSettingsAsync()`

**Example:**
```
User enters: IP="192.168.1.10", Port="70", Nickname="Server A"
Stored as:   "192.168.1.10:70 (Server A)" in history
```

2. **Unique History Entries (No Duplicates)**
   - Connection history now prevents duplicate `IP:Port` combinations
   - When the same IP:Port is saved again, it moves to the top of the list instead of creating a duplicate
   - Unlimited history size (no maxEntries limit by default)
   - Nickname updates are preserved when reusing an entry

**Example:**
```
Before: 192.168.1.10:70, 192.168.1.20:70, 192.168.1.10:70 (duplicate)
After:  192.168.1.10:70 (Server A), 192.168.1.20:70 (Office Server)
```

3. **Refactored Settings Page UI**
   - Page title changed from "Network Settings" to "Settings"
   - Network Settings card at top (IP, Port, Nickname fields + Save button)
   - Connection History card next to it (displays unique IP:Port entries with nicknames)
   - Application Settings card below (Auto-update toggle, Log level dropdown + Save button)
   - HL7 Documentation card and Home button preserved at bottom
   - Two separate save buttons: "Save Network settings" and "Save Application settings"

**Modified Files:**

| File | Changes |
|------|---------|
| `NetworkSettings.cs` | Added `Nickname` property to `NetworkSettings` class; Added `Nickname` property to `ConnectionHistoryEntry` class |
| `NetworkSettingsService.AddToHistory()` | Updated to accept optional nickname parameter; Implemented duplicate detection and removal; Changed maxEntries default to int.MaxValue (unlimited) |
| `ViewModels/NetworkSettingsViewModel.cs` | Added `Nickname` property with binding; Split into `SaveNetworkSettingsAsync()` and `SaveApplicationSettingsAsync()` methods; Updated `LoadAsync()` to load nickname; Added `HistoryEntryViewModel` class for formatted display |
| `NetworkSettingsPage.xaml` | Refactored UI layout with separate Network Settings, Connection History, Application Settings cards; Changed title to "Settings"; Added two separate save buttons |

---

## v2.0.9 Changes

### IP and Port Space Trimming

1. **Leading/Trailing Space Removal**
   - Automatically trims leading and trailing spaces from IP address and port fields
   - Applied during save operation in `NetworkSettingsViewModel.SaveAsync()`
   - Also applied when selecting entries from connection history (`OnSelectHistoryEntry()`)
   - Prevents silent failures when pasting IPs with surrounding whitespace

**Example:**
```
User pastes: " 192.168.1.10 "
Stored as:   "192.168.1.10"
```

### Modified Files:

| File | Changes |
|------|---------|
| `ViewModels/NetworkSettingsViewModel.cs` | Added `.Trim()` calls in `SaveAsync()` method (lines 163-164) and `OnSelectHistoryEntry()` method (lines 200-201) |

---

## v2.0.8 Changes

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