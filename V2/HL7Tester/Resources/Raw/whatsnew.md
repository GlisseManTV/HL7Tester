## v2.0.16 Changes

### Cross-Platform HL7 File Drag & Drop

Added explicit drag-and-drop support for HL7 text files on both Windows and macOS MacCatalyst, replacing platform-dependent native behavior.

**Key Features:**
- **MainPage GeneratedMessage drop zone**: Dropping a supported file into the generated message area replaces `GeneratedMessage` with the file content
- **HL7 Inspector drop zone**: Dropping a supported file into the raw message editor fills `RawMessage` and automatically parses the message
- **Supported file extensions**: `.hl7`, `.h7`, `.txt`, `.msg`, `.dat`, `.edi`, `.log`
- **Single-file import behavior**: If multiple files are dropped, only the first file is imported and the user is informed
- **Safety checks**: Empty files and files larger than 2 MB are rejected with a user-facing message
- **Text normalization**: Imported text is normalized for consistent line endings across platforms

**Technical Details:**
- Added `Hl7FileImportService` in the MAUI app layer to keep platform drag/drop concerns outside `HL7Tester.Core`
- Windows uses `PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync()` to read dropped `StorageFile` items
- MacCatalyst uses `PlatformArgs.DropSession.Items`, `NSItemProvider.LoadFileRepresentationAsync()`, and `LoadDataRepresentationAsync()` fallback
- `DropGestureRecognizer` is attached to the generated message and inspector raw message container borders for consistent target behavior

---