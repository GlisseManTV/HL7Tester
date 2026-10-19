# HL7Tester V2 — Architecture Documentation

## Overview

HL7Tester is a cross-platform .NET MAUI application for generating, sending, and inspecting HL7 v2 messages, with full FHIR R4 support added in Phases 0.5–9. It targets Windows (WinUI) and macOS (Mac Catalyst).

---

## Project Structure

```
V2/
├── HL7Tester/                 # Main MAUI application (UI layer)
│   ├── Platforms/             # Platform-specific code (Windows, MacCatalyst, iOS, Android)
│   ├── Resources/             # Assets, fonts, styles, converters, raw files
│   │   └── Converters/        # IValueConverter implementations
│   ├── Controls/              # Custom MAUI controls (AppDropdown, DropdownButton)
│   ├── Logging/               # FileLoggerProvider
│   ├── Services/              # App-level services (file import, secret store, what's new)
│   ├── ViewModels/            # MVVM ViewModels
│   └── *.xaml / *.xaml.cs     # Pages (MainPage, NetworkSettings, Inspector, FHIR, etc.)
│
├── HL7Tester.Core/            # Shared business-logic library (no MAUI dependency)
│   ├── Class1.cs              # AdtMessageRequest, ObxEntry, AdtMessageGenerator
│   ├── Hl7NetworkSender.cs    # MLLP transport (IHL7NetworkSender)
│   ├── NetworkSettings.cs     # Settings model + FileNetworkSettingsService
│   ├── UpdateChecker.cs       # GitHub release check
│   │
│   ├── Batch/                 # Batch Send engine (stress testing)
│   │   ├── Models/            # WorkflowStep, PatientContext, BatchWorkflow, BatchSendResult
│   │   ├── BatchSendEngine.cs
│   │   ├── BatchTemplateService.cs
│   │   ├── BatchWorkflowTemplates.cs
│   │   └── PatientDataRandomizer.cs
│   │
│   ├── Fhir/                  # FHIR R4 module
│   │   ├── IFhirResourceGenerator.cs   # Abstraction: AdtMessageRequest → FHIR Resource
│   │   ├── FhirR4Generator.cs          # R4 implementation (Firely SDK)
│   │   ├── FhirToHl7Converter.cs       # Reverse: FHIR JSON → AdtMessageRequest
│   │   ├── FhirRestClient.cs           # HTTP client for FHIR REST API
│   │   ├── FhirSettings.cs             # FhirProfile, FhirAuthConfig, FhirAuthType, SecretRef
│   │   ├── FhirTerminologyMapper.cs    # HL7 ↔ FHIR terminology mappings
│   │   ├── FhirMergeConfig.cs          # Configurable $merge parameter names
│   │   └── FhirAuth/
│   │       └── FhirAuthDelegatingHandler.cs  # Bearer/Basic/OAuth2CC/ApiKey auth
│   │
│   ├── Sending/               # Protocol-agnostic send abstraction
│   │   ├── IMessageSender.cs           # Interface: SendAsync(SendRequest) → MessageSendResult
│   │   ├── SenderRouter.cs             # Routes to MLLP or FHIR based on Protocol
│   │   ├── SendRequest.cs              # Unified request (payload + MLLP/FHIR params)
│   │   └── MessageSendResult.cs        # Unified result (MSA/ACK + HTTP status/body)
│   │
│   ├── Security/              # Secret storage abstraction
│   │   └── ISecretStore.cs             # Get/Set/Delete secrets per profile+key
│   │
│   └── Inspector/             # HL7 Message Inspector
│       ├── Models/            # ParsedHL7Message, HL7SegmentModel, HL7FieldModel, etc.
│       └── Services/
│           ├── HL7ParserService.cs     # Raw HL7 → ParsedHL7Message
│           ├── HL7KnowledgeBase.cs     # Segment/field/component metadata
│           ├── HL7VersionDetector.cs   # MSH-12 version detection
│           ├── Hl7MessageExtractor.cs  # ParsedHL7Message → AdtMessageRequest
│           └── XmlHl7ParserService.cs  # XML HL7 variant parser
│
├── HL7Tester.Tests/           # Unit tests (MSTest)
│   ├── FhirConverterTests.cs           # Hl7MessageExtractor + FhirToHl7Converter + JSON validity
│   ├── FhirR4GeneratorTests.cs         # Generator output structure + JSON serialization
│   ├── BatchSendEngineTests.cs
│   ├── BatchTemplateServiceTests.cs
│   ├── PatientDataRandomizerTests.cs
│   ├── NetworkSettingsTests.cs
│   └── XmlHl7ParserServiceTests.cs
│
├── ARCHITECTURE.md            # This file
├── releases.md                # Changelog
└── HL7Tester.V2.slnx          # Solution
```

---

## Architecture Patterns

### MVVM (Model-View-ViewModel)
- **Views**: XAML pages (`MainPage.xaml`, `NetworkSettingsPage.xaml`, `FhirConverterPage.xaml`, etc.)
- **ViewModels**: `MainViewModel`, `NetworkSettingsViewModel`, `FhirConverterViewModel`, `FhirExplorerViewModel`, `ConnectionsViewModel`
- **Models**: `NetworkSettings`, `FhirProfile`, `AdtMessageRequest`, `SendRequest`, etc.

### Dependency Injection
- All services registered in `MauiProgram.cs`
- Constructor injection in ViewModels and services

### Protocol Abstraction
- `IMessageSender` → `SenderRouter` → `IHL7NetworkSender` (MLLP) or `FhirRestClient` (FHIR)
- Callers depend only on the interface; protocol selection is a runtime routing decision

---

## FHIR Components

### IMessageSender (Sending/)
Protocol-agnostic send interface. Both MLLP and FHIR transports implement it:
```csharp
Task<MessageSendResult> SendAsync(SendRequest request, CancellationToken ct);
```

### SenderRouter (Sending/)
Routes a `SendRequest` to the correct transport based on `request.Protocol` ("MLLP" or "FHIR"). Wraps protocol-specific results into the unified `MessageSendResult`.

### FhirRestClient (Fhir/)
Thin `HttpClient` wrapper for FHIR REST interactions (GET/POST/PUT/PATCH/DELETE). Builds the HTTP client per request with:
- Auth via `FhirAuthDelegatingHandler`
- TLS verification toggle (`FhirProfile.TlsVerify`)
- Configurable timeout
- `application/fhir+json` content type
- Response parsing via Firely SDK `FhirJsonDeserializer`

### FhirR4Generator (Fhir/)
Implements `IFhirResourceGenerator`. Maps `AdtMessageRequest.MessageTypeCode` to FHIR resources:
| HL7 Message Type | FHIR Resource |
|---|---|
| ADT A01/A04/A05/A28 | Transaction Bundle (Patient + Encounter) |
| ADT A08/A31 | Patient (PUT) |
| ADT A03 | Encounter (status=finished, PATCH) |
| ADT A02/A09/A10/A21/A22 | Encounter (status=in-progress, PATCH) |
| ADT A18/A40 | Parameters ($merge) |
| ORM O01 | ServiceRequest |
| SIU S12/S13/S14 | Appointment (status=booked) |
| SIU S15 | Appointment (status=cancelled, PATCH) |

Uses `urn:uuid` cross-references inside Bundles; conditional references (`Type?identifier=value`) outside Bundles.

### FhirToHl7Converter (Fhir/)
Reverse converter: FHIR R4 JSON → `AdtMessageRequest`. Supports:
- Bundle (transaction) → ADT A01
- Patient → ADT A08
- Encounter → ADT A03 (finished) / ADT A02 (in-progress)
- ServiceRequest → ORM O01
- Appointment → SIU S12 / SIU S15 (cancelled)

Returns `null` for invalid JSON or unrecognized resource types.

### Hl7MessageExtractor (Inspector/Services/)
Extracts an `AdtMessageRequest` from a `ParsedHL7Message` (output of `HL7ParserService`). Reads MSH, EVN, PID, PV1, MRG, ORC, OBR, SCH, and OBX segments. Tolerates missing segments (leaves fields at defaults).

### FhirAuthDelegatingHandler (Fhir/FhirAuth/)
`DelegatingHandler` that applies authentication to every outgoing FHIR HTTP request. Supports:
- **None**: no auth header
- **BearerToken**: reads token from `ISecretStore`
- **BasicAuth**: username from profile + password from `ISecretStore`
- **OAuth2ClientCredentials**: fetches token from token endpoint, caches until expiry
- **ApiKey**: custom header from `ISecretStore`

### ISecretStore (Security/)
Abstraction for confidential values (tokens, passwords, client secrets). Keyed by `(profileId, key)`. The app ships `InMemorySecretStore` (non-persistent stub); production builds can plug in DPAPI (Windows) or Keychain (macOS).

### FhirTerminologyMapper (Fhir/)
Static mappings between HL7 v2 code systems and FHIR R4 code systems:
- Sex (M/F/O/U) → `AdministrativeGender`
- Patient class (I/E/O) → Encounter class (`IMP`/`AMB`/`VR`)
- Order status (Final/New/etc.) → `RequestStatus`
- Appointment status (Scheduled/etc.) → `AppointmentStatus`

### FhirMergeConfig (Fhir/)
Configures the parameter names for the `$merge` operation (default: `sourcePatientId` / `targetPatientId`). Allows per-server customization.

---

## FHIR Pages

### FhirConverterPage
Bidirectional HL7v2 ⇄ FHIR converter:
- **HL7 → FHIR**: `HL7ParserService.Parse` → `Hl7MessageExtractor.Extract` → `IFhirResourceGenerator.Generate` → `ToJson`
- **FHIR → HL7**: `FhirToHl7Converter.Convert` → `AdtMessageGenerator.Generate`
- Direction toggle (HL7→FHIR / FHIR→HL7)
- Drag-and-drop file import for input
- Accessible from Settings → HL7 Tools

### FhirExplorerPage
FHIR server interaction tool with three modes:
- **Search**: GET `{BaseUrl}/{Type}?params`
- **Read**: GET `{BaseUrl}/{Type}/{Id}`
- **Capabilities**: GET `{BaseUrl}/metadata`

Uses the active `FhirProfile` for connection/auth. Displays response JSON.

### MainPage — FHIR Mode
When `NetworkSettings.Protocol = "FHIR"`:
- Generates HL7 message internally, converts to FHIR JSON via `FhirR4Generator`
- Displays editable JSON editor with validation (`JsonDocument.Parse`)
- Sends via `SenderRouter` → `FhirRestClient`
- Shows HTTP status + OperationOutcome on error

### ConnectionsPage — FHIR Profiles
Manages FHIR connection profiles:
- Create/edit/delete `FhirProfile` entries (name, base URL, auth type, TLS, timeout)
- Select active profile (`ActiveFhirProfileId`)
- Auth configuration UI per type (Bearer, Basic, OAuth2CC, ApiKey)
- Secret entry fields (stored via `ISecretStore`)

---

## Protocol Toggle

The global transport protocol is controlled by `NetworkSettings.Protocol`:
- **"MLLP"** (default): HL7 v2 over TCP with MLLP framing
- **"FHIR"**: FHIR R4 resources over HTTPS/REST

### UI
- **NetworkSettingsPage**: `AppDropdown` to select protocol (MLLP / FHIR)
- **All pages**: Protocol badge (via `ProtocolDisplayConverter`) shows current mode
- **MainPage**: In FHIR mode, the send button uses `SenderRouter` with `Protocol = "FHIR"`

### Routing
`SenderRouter` inspects `SendRequest.Protocol`:
- `"FHIR"` → `FhirRestClient.SendAsync` (HTTP + auth + TLS)
- `"MLLP"` (default) → `IHL7NetworkSender.SendAsync` (TCP + MLLP framing)

---

## Configuration Files

### networksettings.json

```json
{
  "LastIpAddress": "192.168.1.10",
  "LastPort": "70",
  "Nickname": "Server A",
  "Protocol": "FHIR",
  "MessageEncoding": "UTF-8",
  "LogLevel": "Debug",
  "AutoUpdateCheck": false,
  "History": [
    { "Ip": "192.168.1.10", "Port": "70", "Nickname": "Server A" }
  ],
  "InstalledVersion": "2.0.24",
  "LastShownWhatNewVersion": "2.0.24",
  "ActiveFhirProfileId": "abc123def456",
  "FhirProfiles": [
    {
      "Id": "abc123def456",
      "Name": "HAPI FHIR Demo",
      "BaseUrl": "https://hapi.fhir.org/baseR4",
      "Auth": { "Type": "None" },
      "DefaultEndpoint": "Patient",
      "TlsVerify": true,
      "TimeoutSeconds": 30,
      "FhirVersion": "4.0",
      "Secrets": []
    }
  ],
  "SettingsSchemaVersion": 2
}
```

### Settings Schema Versioning
- **v1** (legacy): No `Protocol`, `FhirProfiles`, `ActiveFhirProfileId`, or `SettingsSchemaVersion` fields. Deserializes with defaults (Protocol = "MLLP").
- **v2** (current): Includes all FHIR fields. Written on save.

---

## Testing

### Test Framework
- MSTest 4.0.1 with implicit `using Microsoft.VisualStudio.TestTools.UnitTesting`
- Test project: `HL7Tester.Tests/`
- Target: `net10.0`

### Test Coverage (FHIR module)

| File | Tests |
|------|-------|
| `FhirConverterTests.cs` | 15 tests: Hl7MessageExtractor (6), FhirToHl7Converter (7), FhirR4Generator JSON validity (2) |
| `FhirR4GeneratorTests.cs` | 12 tests: Generate for each message type + ToJson serialization |
| `BatchSendEngineTests.cs` | FHIR protocol routing tests included |

### Test Patterns
- HL7 messages in tests use realistic but minimal segments
- FHIR JSON tests use `FhirR4Generator` round-trip to ensure valid input
- Parser tests instantiate `HL7ParserService` directly (no DI)

---

## Development Guidelines

### Language Policy: ENGLISH ONLY
All code, comments, labels, and documentation must be in English.

### Code Quality Standards
1. **Async/Await**: Use `.ConfigureAwait(false)` in library code
2. **Null Safety**: Nullable reference types enabled (`<Nullable>enable</Nullable>`)
3. **ILogger**: DI-injected `ILogger<T>` for all logging
4. **MVVM**: UI logic in Views, business logic in ViewModels/Services
5. **No MAUI dependency in Core**: `HL7Tester.Core` must not reference MAUI

### Key NuGet Dependencies
| Package | Purpose |
|---------|---------|
| `Hl7.Fhir.R4` 6.7.0 | Firely SDK: FHIR R4 models + serialization |
| `NHapi` | HL7 v2.3 message generation (ADT/ORM/SIU) |
| `MSTest` 4.0.1 | Unit test framework |

---

*Last Updated: October 18, 2026 (Phase 10 — Tests + Documentation)*
