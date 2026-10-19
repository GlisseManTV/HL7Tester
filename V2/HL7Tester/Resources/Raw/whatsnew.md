## v2.0.25

### 📡 Connections Have Their Own Page
> Network settings moved to a dedicated page for a cleaner experience.
- IP, Port, and saved connections now live in "Connections"
- Access it from Settings or the migration card
- Same functionality, better organization

### ⚠️ FHIR Support — Beta
> FHIR protocol support is new in this release and still in beta.
- Features work but are not yet optimized
- Expect changes and improvements in upcoming versions
- Feedback welcome — report any issues you encounter

### 🔌 FHIR Protocol Support
> Send your HL7 messages as FHIR REST calls — same app, new protocol.
- Toggle between HL7v2/MLLP and FHIR in Settings
- Generate FHIR Bundles, Patient, Encounter from the same patient fields
- Works on Windows and macOS

### 🔐 FHIR Server Profiles
> Connect to any FHIR server with the right authentication.
- Bearer token, Basic auth, OAuth2, or API key
- Multiple profiles, pick the active one
- TLS verification toggle

### 🔄 HL7 ⇄ FHIR Converter
> Convert between HL7v2 and FHIR JSON in one click.
- Paste an HL7 message → get FHIR JSON
- Paste FHIR JSON → get HL7v2
- Chain conversions back and forth

### 🔍 FHIR Explorer
> Browse what's on your FHIR server.
- Search patients, encounters, and more with query params
- Read a specific resource by ID
- Discover server capabilities with one click

### ✏️ JSON Editor
> Edit the generated JSON before sending.
- Modify, paste, or format JSON directly
- Validate syntax before sending
- Check server-side validation with $validate
