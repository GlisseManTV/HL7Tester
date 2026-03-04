# HL7Tester v2 — Windows installer (MSI) & signing notes

This project is a **.NET MAUI** app.

For Windows distribution outside Microsoft Store you typically have 2 steps:
1) produce a **Windows publish folder** (unpackaged)
2) create an **installer** (MSI / setup.exe) from that folder

> Note: a certificate provided by a domain hoster is often a **TLS/SSL certificate** (for HTTPS). It usually **cannot sign code** (Authenticode). For signing a `.msi`/`.exe` you need a **Code Signing** certificate (OV/EV) that you can export as `.pfx`.

---

## 1) Build/publish (Windows, unpackaged)

### Prerequisites
- Windows 10/11
- .NET SDK (same major as the project; currently `net10.0`)

### Publish command (x64)

```powershell
dotnet publish .\V2\HL7Tester\HL7Tester.csproj `
  -c Release -f net10.0-windows10.0.19041.0 `
  -p:WindowsPackageType=None `
  -r win-x64 --self-contained true `
  -o .\artifacts\win-x64
```

### Publish command (ARM64)

```powershell
dotnet publish .\V2\HL7Tester\HL7Tester.csproj `
  -c Release -f net10.0-windows10.0.19041.0 `
  -p:WindowsPackageType=None `
  -r win-arm64 --self-contained true `
  -o .\artifacts\win-arm64
```

Important properties:
- `WindowsPackageType=None` => unpackaged (no MSIX)
- `--self-contained true` => bundles the .NET runtime
- `WindowsAppSDKSelfContained=true` (set in `HL7Tester.csproj`) => bundles Windows App SDK runtime so the app can start without installing Windows App Runtime.

---

## 2) If the published EXE does not start

Typical crash:
- `Faulting module: Microsoft.UI.Xaml.dll` / exception `0xc000027b`

What to check on the target machine:

### Check Windows App Runtime
```powershell
Get-AppxPackage Microsoft.WindowsAppRuntime* | Select Name, Version
```

### Check architecture
Make sure you published `win-x64` for an x64 machine.

### VC++ runtime
Some configurations require **Microsoft Visual C++ Redistributable 2015-2022**.
If needed, install it (x64) from Microsoft.

---

## 3) MSI creation (recommended: WiX Toolset v4)

At a high level your WiX project should:
- install everything from `artifacts\win-x64\*` into `ProgramFilesFolder\HL7Tester\`
- add Start Menu shortcut
- optionally add Desktop shortcut

Once your WiX project produces `HL7Tester.msi`, you can sign it.

---

## 3bis) Inno Setup (EXE installer) — what to package?

Yes: **target the whole publish folder** (for example `artifacts\win-x64\`).

For MAUI/WinUI apps, the `.exe` depends on many DLLs next to it.
If you only include `HL7Tester.exe`, it will break on the target machine.

### Minimal Inno Setup example

```ini
[Setup]
AppId={{YOUR-GUID-HERE}}
AppName=HL7Tester
AppVersion=2.0.1
AppPublisher=ItConsult4Care
DefaultDirName={autopf}\HL7Tester
DefaultGroupName=HL7Tester
OutputBaseFilename=HL7Tester_Setup
Compression=lzma
SolidCompression=yes

[Files]
; include EVERYTHING from the publish folder
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; for ARM64 builds, point to artifacts\win-arm64 instead
; Source: "..\artifacts\win-arm64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\HL7Tester"; Filename: "{app}\HL7Tester.exe"; WorkingDir: "{app}"
Name: "{commondesktop}\HL7Tester"; Filename: "{app}\HL7Tester.exe"; Tasks: desktopicon; WorkingDir: "{app}"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked
```

Notes:
- Make the `Source:` path relative to your `.iss` file.
- Your `AppId` should be stable across releases (so upgrades work).
- Keep the install folder out of OneDrive paths.

### Signing the Inno Setup output
Inno Setup can run a post-compile step to sign the generated setup exe using `signtool`.
You still need a **real code-signing certificate (PFX)**.

---

## 4) Signing (Authenticode)

### Using signtool (Windows SDK)

```powershell
$Pfx = "C:\certs\codesign.pfx"
$Pwd = "YOUR_PASSWORD"
$Ts  = "http://timestamp.digicert.com"

# sign the main exe
signtool sign /fd SHA256 /td SHA256 /tr $Ts /f $Pfx /p $Pwd .\artifacts\win-x64\HL7Tester.exe

# sign the MSI
signtool sign /fd SHA256 /td SHA256 /tr $Ts /f $Pfx /p $Pwd .\dist\HL7Tester.msi
```

### SmartScreen note
Even with an OV code-signing certificate, SmartScreen can still warn until your downloads build reputation.
EV certificates reduce this effect.
