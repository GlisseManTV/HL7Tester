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
