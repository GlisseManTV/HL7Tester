## v2.0.17

### 🔄 Regenerate ControlID Without Changing Anything Else
> Reuse an existing message by refreshing only its ControlID — no more rejected duplicates.
- Click "Refresh ID" to generate a new unique ControlID (MSH-10)
- The rest of the message stays exactly as it was
- Uses the same SHA256 algorithm as the original generator for full consistency

### 📂 Drop HL7 Files Directly into the App
> No more copy-paste. Just drag a file and you're done.
- Works on both Windows and macOS
- Supported formats: .hl7, .txt, .msg, .dat and more
- Dropped files are auto-parsed in the Inspector
- Large or empty files are safely rejected

### 🔍 Smarter Inspector Integration
> The Inspector now accepts dropped files and parses them instantly.
- Drop a file → message parsed automatically
- Send any inspected message back to the main screen in one click
- Works seamlessly with all supported file types
