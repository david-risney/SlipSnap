# SlipSnap

A small Windows utility that displays floating toolbar windows that snap to the edges of your monitor. Designed for use with fullscreen apps that capture all input (like Remote Desktop), SlipSnap provides quick access to:

- **Start menu** invocation
- **Task view** invocation
- **Next / Previous virtual desktop** switching (host)
- **Next / Previous virtual desktop** switching (remote/inner session)

The toolbars can optionally auto-hide when not displayed over a fullscreen window, staying out of your way until you need them.

---

## Technology Stack

- **C# / WPF (.NET 8)** — Chosen for easy topmost window management, Win32 interop via P/Invoke, and COM interop for virtual desktop APIs.
- **Target framework**: `net8.0-windows10.0.22621.0` (Windows 10 22H2+) — required by the `Slions.VirtualDesktop.WPF` NuGet package.
- **Key dependencies**:
  - `Slions.VirtualDesktop.WPF` (6.9.2) — Pin window to all virtual desktops via `window.Pin()`. This is a maintained fork of `Grabacr07/VirtualDesktop`. Note: uses undocumented COM interfaces that can break between Windows builds.

## Architecture Decisions

### Staying on Top of Fullscreen RDP

The core challenge: Remote Desktop's fullscreen window is itself `TOPMOST`, so a normal `Topmost=True` WPF window gets hidden behind it.

**Approaches evaluated:**

| Approach | Outcome |
|---|---|
| `SetWindowPos(HWND_TOPMOST)` on a timer | Works but is a polling hack / race condition. RDP can reclaim the top Z-order between polls. **Rejected.** |
| `SetWinEventHook` (EVENT_SYSTEM_FOREGROUND) | Event-driven version of the timer. Same fundamental Z-order ceiling. **Rejected.** |
| Magnification API overlay | Guaranteed by DWM, but designed for screen magnification — would need to composite UI onto a 1:1 magnified view. Medium complexity. **Not pursued.** |
| RDP Virtual Channel Plugin | Runs inside the RDP session. Major complexity increase. **Not pursued.** |
| **`UIAccess=true` in app manifest** | **Chosen.** Windows grants a higher Z-order band than normal topmost windows. This is how `osk.exe` (On-Screen Keyboard) works. No polling needed. |

### UIAccess Requirements

UIAccess is enforced by the Windows kernel at process launch. **All three** must be satisfied:

1. **App manifest** declares `uiAccess="true"` in `requestedExecutionLevel`
2. **Authenticode signature** on the .exe — the cert must chain to a root in `Cert:\LocalMachine\Root`
3. **Trusted location** — exe must be in `C:\Program Files\`, `C:\Program Files (x86)\`, or `C:\Windows\System32\`

If any check fails, UIAccess is **silently stripped** — the app launches normally without the elevated Z-order. No error is shown.

**App manifest** (`app.manifest`):
```xml
<requestedExecutionLevel level="highestAvailable" uiAccess="true" />
```
Note: the Microsoft docs example uses `level="highestAvailable"`. We initially used `level="asInvoker"` — both work for UIAccess, but `highestAvailable` is what the docs recommend for assistive tech.

### Host vs Remote Desktop Switching

Both host and remote desktop switching use the same keystroke (`Ctrl+Win+Left/Right`), but the destination depends on **which process has keyboard focus** when the keys are injected:

- **Host buttons**: Call `SetForegroundWindow(ourWindow)` first to take focus away from RDP, then `keybd_event` sends `Ctrl+Win+Arrow`. The host shell intercepts it.
- **Remote buttons**: Must **not** take focus. RDP keeps focus and forwards the injected keystrokes to the remote session.

### Window Non-Activation Strategy

WPF's button click handling activates the parent window through internal code paths. To prevent this for remote buttons:

- `WM_MOUSEACTIVATE → MA_NOACTIVATE` is returned from the window proc, but **only when `_blockActivation` is true** (set per-button-click).
- Host buttons set `_blockActivation = false` and explicitly call `SetForegroundWindow`.
- Remote buttons set `_blockActivation = true` so clicking them doesn't steal focus from RDP.

**Previously tried and rejected:**
- `WS_EX_NOACTIVATE` extended window style on the whole window — this made all buttons non-activating, which broke host desktop switching (the shell still intercepted `Ctrl+Win+Arrow` because it's a global hotkey, but other host actions like Start Menu and Task View stopped working correctly).

### Post-Switch Focus Handoff

After a host desktop switch, if SlipSnap has focus and there's a fullscreen window on the new desktop, focus is automatically handed to that fullscreen window. This prevents SlipSnap from "stealing" focus after switching to a desktop with a fullscreen RDP session.

Implementation: `EnumWindows` + `GetWindowRect` + `MonitorFromWindow`/`GetMonitorInfo` to find a window that covers an entire monitor, then `SetForegroundWindow` on it. Runs on `DispatcherPriority.Background` to let the desktop switch settle first.

### Pinning to All Virtual Desktops

Uses `Slions.VirtualDesktop.WPF` package's `window.Pin()` method, which calls the undocumented `IVirtualDesktopPinnedApps::PinWindow` COM interface. Called once in `Window_Loaded`.

## Issues Encountered & Workarounds

### 1. MSIX + UIAccess is Incompatible

**Problem:** MSIX-packaged app fails to launch with "The request is not supported" error dialog.

**Root cause:** MSIX app activation uses `CreateProcess`-like internal APIs. UIAccess-enabled programs **cannot** be launched via `CreateProcess` — they require `ShellExecute`. This fails with `ERROR_ELEVATION_REQUIRED` internally.

**Additionally:** Declaring `rescap:Capability Name="uiAccess"` in `AppxManifest.xml` causes MSIX installation to fail with "Access is denied" during `windows.uiAccess` extension registration. This is a restricted capability requiring Microsoft Store approval or enterprise sideloading policy.

**Workaround:** Abandoned MSIX packaging. Deploy as a signed exe to `C:\Program Files\SlipSnap\` directly. Microsoft PowerToys has the same open issue (PowerToys#15241) — they haven't solved it either.

**Key reference:** GitHub user @gexgd0419 in PowerToys#15241: *"UIAccess-enabled programs cannot be launched using CreateProcess. This will fail with ERROR_ELEVATION_REQUIRED. ShellExecute still works."*

### 2. Timer-Based Z-Order Re-assertion

**Problem:** Initial POC used `SetWindowPos(HWND_TOPMOST)` on a 500ms `DispatcherTimer` to stay above RDP.

**Why it's wrong:** Polling is a race condition. RDP can reclaim topmost between polls, causing flicker.

**Fix:** Replaced with `UIAccess=true` manifest approach. No polling needed — OS natively grants higher Z-order band.

### 3. WPF Button Clicks Activate Window Despite WM_MOUSEACTIVATE

**Problem:** Returning `MA_NOACTIVATE` from `WM_MOUSEACTIVATE` was insufficient. WPF's internal button click handling still activated the window through other code paths.

**Attempted fix:** Set `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` extended window styles at the Win32 level. This made the window truly non-activating.

**New problem:** With global non-activation, `SetForegroundWindow(ourWindow)` for host buttons may silently fail because Windows respects the `WS_EX_NOACTIVATE` style.

**Final approach:** Conditional `WM_MOUSEACTIVATE` handling via a `_blockActivation` flag toggled per-button-click, without `WS_EX_NOACTIVATE` on the window.

### 4. Exe Signing vs MSIX Signing

**Problem:** Initially only signed the MSIX package, not the exe inside it. UIAccess validation checks the **exe's own** Authenticode signature, not the outer package signature.

**Fix:** Sign the exe with `signtool` before packaging into MSIX.

### 5. Certificate Store for UIAccess

**Problem:** Initially imported the dev cert to `Cert:\LocalMachine\TrustedPeople` only. UIAccess requires the cert to chain to a root in `Cert:\LocalMachine\Root`.

**Fix:** Import to both `Root` (for UIAccess) and `TrustedPeople` (for MSIX sideloading).

## Build & Deploy

### Prerequisites
- .NET 8 SDK
- Windows SDK (for `signtool.exe`)
- Self-signed code-signing certificate (created automatically by `Build-Msix.ps1` on first run)

### Dev Build (no UIAccess)
```powershell
dotnet build
dotnet run
```

### Signed Deploy (UIAccess works)
```powershell
# Publish
dotnet publish -c Release -r win-x64 --self-contained -o publish

# Sign
$t = (Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq "CN=SlipSnap Dev" }).Thumbprint
& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe" sign /fd SHA256 /sha1 $t publish\SlipSnap.exe

# Deploy to Program Files (admin UAC prompt)
Start-Process pwsh -Verb RunAs -ArgumentList '-Command', "Copy-Item 'publish\*' 'C:\Program Files\SlipSnap\' -Recurse -Force" -Wait

# Launch
& "C:\Program Files\SlipSnap\SlipSnap.exe"
```

### First-Time Cert Setup (admin, one-time)
```powershell
Import-Certificate -FilePath dev-cert.cer -CertStoreLocation Cert:\LocalMachine\Root
Import-Certificate -FilePath dev-cert.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
```

## Distribution Options

| Approach | Cost | Notes |
|---|---|---|
| Self-signed cert + manual install | Free | Dev/personal use only. Requires trusting cert on each machine. |
| Certum open-source OV cert | ~$27/year | Cheapest for open-source projects. |
| Standard OV code-signing cert | ~$70-300/year | Sectigo, DigiCert, etc. |
| EV code-signing cert | ~$300-600/year | Immediate SmartScreen trust. Best UX. |
| MSIX / Microsoft Store | Free (Microsoft signs) | **Not viable** — UIAccess is incompatible with MSIX activation. |

## Key Win32 APIs Used

- `keybd_event` — Simulate keyboard input for Start Menu, Task View, and desktop switching
- `SetForegroundWindow` — Manage focus for host vs remote button behavior
- `GetForegroundWindow` — Detect current foreground window
- `EnumWindows` / `GetWindowRect` / `MonitorFromWindow` / `GetMonitorInfo` — Detect fullscreen windows for post-switch focus handoff
- `IVirtualDesktopPinnedApps::PinWindow` (via Slions.VirtualDesktop) — Pin to all desktops
