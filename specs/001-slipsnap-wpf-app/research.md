# Research: SlipSnap WPF Floating Toolbar

**Feature**: 001-slipsnap-wpf-app  
**Date**: 2026-04-02

## 1. Modern WPF UI Library

**Decision**: WPF UI (wpfui) v4.2.0

**Rationale**:
- Provides Windows 11 Fluent Design controls natively in WPF.
- Targets .NET 8 and .NET Framework 4.6.2+. MIT license.
- Includes built-in tray icon (`ui:TrayIcon`) with context menu — eliminates need for a separate tray icon library.
- Includes `ThemesDictionary` with Dark/Light/Auto theme switching via `ApplicationThemeManager`.
- Active maintenance (788K+ NuGet downloads, last updated 3 months ago).
- Works with Visual Studio XAML designer.

**Alternatives considered**:
- **ModernWpf**: Less active, fewer controls. WPF UI has better Fluent Design parity.
- **MahApps.Metro**: Older Metro aesthetic, not Windows 11 Fluent Design.
- **WinUI 3 (Windows App SDK)**: Would require abandoning WPF entirely. UIAccess compatibility unknown.

**NuGet**: `WPF-UI` v4.2.0  
**Namespace**: `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"`

## 2. System Tray Icon

**Decision**: Use WPF UI's built-in tray icon capability (not a separate library).

**Rationale**:
- WPF UI already includes custom tray icon and menu support in pure WPF.
- Eliminates H.NotifyIcon.Wpf as a dependency (Constitution V: prefer built-in over third-party).
- One less NuGet package to maintain compatibility with.

**Alternatives considered**:
- **H.NotifyIcon.Wpf** (v2.4.1): Full-featured but latest version targets .NET 10. Would add an unnecessary dependency when WPF UI already covers this.
- **System.Windows.Forms.NotifyIcon**: WinForms interop in WPF — works but feels foreign.

## 3. UI Automation Testing Framework

**Decision**: FlaUI v5.0.0 (FlaUI.UIA3)

**Rationale**:
- Purpose-built for .NET UI Automation testing. Targets .NET 6+ (compatible with .NET 8).
- Supports UIA3 (the modern UI Automation architecture).
- Clean API for finding elements, clicking buttons, reading properties, and simulating input.
- 3.1M+ NuGet downloads. Active maintenance.
- Works with WPF applications out of the box — WPF generates UIA accessibility tree natively.

**Alternatives considered**:
- **Appium + WinAppDriver**: Heavier setup, requires WinAppDriver service. Overkill for a single WPF app.
- **Microsoft.VisualStudio.TestTools.UITesting (Coded UI)**: Deprecated.
- **Raw System.Windows.Automation**: Low-level, verbose. FlaUI wraps this cleanly.

**NuGet**: `FlaUI.UIA3` v5.0.0

## 4. Screenshot Comparison for Visual Tests

**Decision**: Custom bitmap comparison using System.Drawing or ImageSharp.

**Rationale**:
- Screenshot tests need to capture the toolbar, apply a pixel-diff threshold, and compare against a baseline.
- No heavy image processing library needed — simple per-pixel comparison with tolerance.
- FlaUI provides `Capture.Screen()` and `Capture.Element()` for screenshots.
- Custom `ScreenshotComparer` helper keeps it minimal (Constitution V).

**Alternatives considered**:
- **Verify.ImageSharp**: Full snapshot testing framework, but heavy for our needs.
- **Codeuctivity.ImageSharpCompare**: Good library but adding it for a few comparison calls is unnecessary when FlaUI + System.Drawing handles it.

## 5. Virtual Desktop API

**Decision**: Slions.VirtualDesktop.WPF v6.9.2 (validated in prototype)

**Rationale**:
- Prototype already validated `window.Pin()` and desktop switching.
- Maintained fork of Grabacr07/VirtualDesktop — the most actively maintained option.
- Requires `net8.0-windows10.0.22621.0` target framework.
- Uses undocumented IVirtualDesktop COM interfaces — must be abstracted behind `IVirtualDesktopService` (Constitution I & Platform Constraints).

**Alternatives considered**:
- **MScholtes/VirtualDesktop**: Another wrapper, but less actively maintained and doesn't offer WPF window pinning.
- **Direct COM interop**: Maximum control but high implementation cost and fragility.

**Risk**: COM interfaces may break between Windows builds. Mitigation: version-check at startup, disable desktop buttons gracefully if API unavailable.

## 6. UIAccess Strategy (validated in prototype)

**Decision**: `UIAccess=true` in app manifest + Authenticode signing + Program Files deployment.

**Rationale** (from prototype research):
- Only approach that reliably renders above fullscreen RDP windows.
- No polling, no timer-based Z-order reassertion needed.
- Prototype confirmed all three requirements: manifest, signing, trusted location.
- MSIX is incompatible (CreateProcess limitation).

**Key implementation details**:
- `app.manifest`: `<requestedExecutionLevel level="highestAvailable" uiAccess="true" />`
- Exe must be signed with Authenticode cert trusted in `LocalMachine\Root`.
- Deploy to `C:\Program Files\SlipSnap\`.
- Self-signed cert for dev, CA cert for production.

## 7. Diagnostic Logging

**Decision**: Serilog with Serilog.Sinks.File

**Rationale**:
- Lightweight, widely used, supports rolling file with size-based rotation.
- Integrates with `Microsoft.Extensions.Logging` via `Serilog.Extensions.Logging` if we want DI integration.
- Configuration is a few lines of code.

**Alternatives considered**:
- **NLog**: Similar capability, slightly heavier configuration.
- **Microsoft.Extensions.Logging with custom file provider**: No built-in file sink; would need manual implementation. Serilog is simpler.
- **Raw `File.AppendAllText`**: Too primitive for structured logs with rotation.

**NuGet**: `Serilog.Sinks.File` + `Serilog.Extensions.Logging`

## 8. Settings Persistence

**Decision**: System.Text.Json (built-in) with a JSON file at `%AppData%\SlipSnap\settings.json`.

**Rationale**:
- System.Text.Json is included in .NET 8 — no additional dependency.
- Source-generated serialization available for AOT compatibility and performance.
- JSON is human-readable, debuggable, and trivially backed up.
- File-based approach allows xcopy backup/restore of settings.

**Alternatives considered**:
- **Newtonsoft.Json**: Unnecessary dependency when System.Text.Json is built-in.
- **Windows Registry**: Not human-readable, harder to debug, not portable.
- **SQLite**: Overkill for a flat settings file.

## 9. Fullscreen Detection

**Decision**: `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` + `EnumWindows`/`GetWindowRect`/`GetMonitorInfo`

**Rationale** (from prototype research):
- Event-driven: `SetWinEventHook` fires when foreground window changes — no polling (Constitution IV).
- On each foreground change, enumerate top-level windows and check if any covers the full monitor area.
- Catches both exclusive fullscreen and borderless fullscreen (confirmed with RDP in prototype).
- `EnumWindows` + `GetWindowRect` + `MonitorFromWindow` + `GetMonitorInfo` — all standard Win32 APIs.

**Alternatives considered**:
- **Polling timer**: Violates Constitution IV (no background polling loops).
- **`SHQueryUserNotificationState`**: Only detects presentation mode, not arbitrary fullscreen windows.

## 10. RDP Session Detection

**Decision**: `GetSystemMetrics(SM_REMOTESESSION)` or `System.Windows.Forms.SystemInformation.TerminalServerSession`

**Rationale**:
- Single Win32 call returns `true` if running in an RDP session. Zero overhead.
- Standard documented API — will not break between Windows versions.
- Check once at startup and optionally on `WM_WTSSESSION_CHANGE` for session changes.

**Alternatives considered**:
- **WTSQuerySessionInformation**: More detailed but unnecessary — we only need a boolean.
- **Environment variable check**: Fragile, undocumented.

## Summary of Dependency Stack

| Package | Version | Purpose |
|---------|---------|---------|
| WPF-UI | 4.2.0 | Fluent Design controls, theming, tray icon |
| Slions.VirtualDesktop.WPF | 6.9.2 | Virtual desktop pinning and switching |
| Serilog.Sinks.File | latest | Rolling file log sink |
| Serilog.Extensions.Logging | latest | Microsoft.Extensions.Logging integration |
| xunit | latest | Unit test framework |
| FluentAssertions | latest | Test assertion library |
| FlaUI.UIA3 | 5.0.0 | End-to-end UI Automation tests |
