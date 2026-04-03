# Implementation Plan: SlipSnap WPF Floating Toolbar

**Branch**: `001-slipsnap-wpf-app` | **Date**: 2026-04-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-slipsnap-wpf-app/spec.md`

## Summary

Build a WPF desktop application (.NET 8) that displays semi-transparent floating toolbar windows snapped to monitor edges, rendered above all other windows (including fullscreen RDP) via UIAccess=true manifest. The toolbar provides Start Menu, Task View, and virtual desktop navigation buttons. A modern settings dialog (WPF UI / Fluent Design) lets users configure theme, transparency, toolbar placement, button assignment, fullscreen-only mode, and RDP session detection. Settings are stored in a JSON file. Comprehensive automated tests use UI Automation and screenshot comparison.

## Technical Context

**Language/Version**: C# 13 / .NET 8 (target `net8.0-windows10.0.22621.0`)
**Primary Dependencies**:
- WPF (built-in) — window management, XAML UI
- WPF UI (wpfui v4.2.0, NuGet) — Windows 11 Fluent Design controls, theming (Dark/Light/Auto), and built-in tray icon for settings dialog
- Slions.VirtualDesktop.WPF (v6.9.2, NuGet) — pin window to all virtual desktops, desktop switching API
- System.Text.Json (built-in) — settings serialization
- Serilog.Sinks.File + Serilog.Extensions.Logging (NuGet) — diagnostic logging with file rotation

**Storage**: JSON file at `%AppData%\SlipSnap\settings.json`
**Testing**:
- xUnit + FluentAssertions — unit tests
- FlaUI (NuGet) — UI Automation end-to-end tests
- ImageSharpCompare or custom Bitmap comparison — screenshot verification tests

**Target Platform**: Windows 10 22H2+ (build 22621), Windows 11
**Project Type**: Desktop app (WPF)
**Performance Goals**: Cold start < 2s, button response < 1s, settings apply < 1s
**Constraints**: < 50 MB memory steady state, no polling loops, UIAccess requires Authenticode + Program Files deployment
**Scale/Scope**: Single-monitor v1, up to 4 simultaneous toolbars

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Automated Testing (NON-NEGOTIABLE) | ✅ PASS | FR-016 requires comprehensive tests. Plan includes xUnit unit tests with interface abstractions for Win32/COM, plus FlaUI E2E tests and screenshot tests. |
| II. Code Quality | ✅ PASS | .NET 8 with nullable refs, warnings-as-errors, `.editorconfig`. P/Invoke in `NativeMethods`. One class per file. DI via composition. |
| III. User Experience Consistency | ✅ PASS | FR-015 (no focus stealing), FR-017 (immediate settings apply), FR-004 (edge-adaptive layout). Prototype validated non-activation strategy. |
| IV. Performance Requirements | ✅ PASS | SC-004 < 2s start, SC-007 < 50 MB. Event-driven fullscreen detection via `SetWinEventHook`. No polling. Deterministic COM cleanup. |
| V. Simplicity & Minimalism | ✅ PASS | Single-project structure. Third-party libs only where built-in is insufficient (WPF UI for Fluent Design, FlaUI for UIA tests). Xcopy deploy to Program Files. |
| Platform & Security | ✅ PASS | UIAccess manifest, Authenticode, trusted location. COM abstracted behind interface. Self-signed cert for dev. |

**Gate result: PASS** — proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-slipsnap-wpf-app/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── SlipSnap/                      # Main WPF application project
│   ├── App.xaml(.cs)              # Application entry, tray icon setup, DI container
│   ├── app.manifest               # UIAccess=true manifest
│   ├── Models/
│   │   ├── ToolbarEdge.cs         # Enum: Left, Right, Top, Bottom
│   │   ├── ToolbarButtonType.cs   # Enum: StartMenu, TaskView, NextDesktop, PrevDesktop
│   │   ├── ToolbarConfig.cs       # Per-toolbar: edge, enabled buttons, position
│   │   └── AppSettings.cs         # Root settings: theme, transparency, toolbar configs, flags
│   ├── Services/
│   │   ├── ISettingsService.cs    # Load/save JSON settings
│   │   ├── SettingsService.cs
│   │   ├── IKeyboardSimulator.cs  # Abstract keybd_event
│   │   ├── KeyboardSimulator.cs
│   │   ├── IVirtualDesktopService.cs  # Abstract desktop switching + pinning
│   │   ├── VirtualDesktopService.cs
│   │   ├── IFullscreenDetector.cs # Abstract fullscreen window detection
│   │   ├── FullscreenDetector.cs
│   │   ├── IRdpSessionDetector.cs # Abstract RDP session detection
│   │   ├── RdpSessionDetector.cs
│   │   ├── IToolbarManager.cs     # Create/destroy/reposition toolbars
│   │   └── ToolbarManager.cs
│   ├── Interop/
│   │   └── NativeMethods.cs       # All P/Invoke declarations, Win32 constants
│   ├── Views/
│   │   ├── ToolbarWindow.xaml(.cs)    # Toolbar overlay window
│   │   └── SettingsWindow.xaml(.cs)   # Modern settings dialog (WPF UI)
│   ├── ViewModels/
│   │   ├── ToolbarViewModel.cs
│   │   └── SettingsViewModel.cs
│   └── SlipSnap.csproj
│
tests/
├── SlipSnap.Tests/                # Unit tests
│   ├── Services/
│   │   ├── SettingsServiceTests.cs
│   │   ├── FullscreenDetectorTests.cs
│   │   ├── RdpSessionDetectorTests.cs
│   │   └── ToolbarManagerTests.cs
│   ├── ViewModels/
│   │   ├── ToolbarViewModelTests.cs
│   │   └── SettingsViewModelTests.cs
│   └── SlipSnap.Tests.csproj
│
├── SlipSnap.E2ETests/             # End-to-end UIA + screenshot tests
│   ├── ToolbarVisibilityTests.cs
│   ├── ToolbarButtonTests.cs
│   ├── ToolbarDragTests.cs
│   ├── SettingsDialogTests.cs
│   ├── TrayIconTests.cs
│   ├── ScreenshotTests.cs
│   ├── Helpers/
│   │   ├── AppLauncher.cs         # Start/stop SlipSnap for tests
│   │   └── ScreenshotComparer.cs  # Bitmap comparison helper
│   └── SlipSnap.E2ETests.csproj
```

**Structure Decision**: Single WPF project with MVVM pattern. Services layer abstracts all Win32/COM interop behind interfaces for testability (Constitution I). Separate unit test and E2E test projects. No multi-project overkill — one app, two test projects.

## Complexity Tracking

No constitution violations. Table not needed.
