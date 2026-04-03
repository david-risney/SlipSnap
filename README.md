# SlipSnap

A floating toolbar for Windows that stays on top of fullscreen applications — including Remote Desktop sessions. Provides quick access to Start Menu, Task View, and virtual desktop switching without stealing focus.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4) ![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Floating toolbar** snapped to any monitor edge (left, right, top, bottom)
- **Stays above fullscreen windows** including RDP client via UIAccess manifest
- **Virtual desktop navigation** — switch desktops without leaving your fullscreen app
- **Start Menu & Task View** buttons accessible even over fullscreen windows
- **Drag grip** to reposition the toolbar along the edge
- **Multiple toolbars** — configure independent toolbars on each edge with different button sets
- **Modern settings dialog** with Windows 11 Fluent Design (WPF UI)
- **Theme support** — Auto, Light, or Dark mode
- **Adjustable transparency** — 10% to 100% opacity
- **Fullscreen-only mode** — hide toolbars when no fullscreen window is present
- **RDP session detection** — auto-hide when running inside a remote session
- **Pinned to all virtual desktops** — always available regardless of which desktop is active
- **System tray icon** with Settings and Quit actions
- **Diagnostic logging** to `%AppData%\SlipSnap\logs\`

## Requirements

- Windows 10 22H2+ (build 22621) or Windows 11
- .NET 8 Desktop Runtime

### UIAccess (optional but recommended)

For the toolbar to render above fullscreen windows, the app must:

1. Be **Authenticode signed** (self-signed is sufficient for local use)
2. Run from a **trusted location** (e.g., `C:\Program Files\SlipSnap\`)

Without these, the toolbar still works but may not appear above fullscreen apps.

## Getting Started

### Build

```powershell
dotnet build src/SlipSnap.sln
```

### Run

```powershell
dotnet run --project src/SlipSnap
```

### Test

```powershell
# Unit tests (52 tests)
dotnet test tests/SlipSnap.Tests

# E2E tests (require signed and deployed app)
dotnet test tests/SlipSnap.E2ETests
```

## Project Structure

```
src/
└── SlipSnap/                     # Main WPF application
    ├── App.xaml(.cs)             # Entry point, service wiring, tray icon
    ├── app.manifest              # UIAccess=true manifest
    ├── Assets/                   # App icon
    ├── Interop/NativeMethods.cs  # Win32 P/Invoke declarations
    ├── Models/                   # AppSettings, ToolbarConfig, enums
    ├── Services/                 # Settings, keyboard, virtual desktop,
    │                             #   fullscreen detection, RDP detection,
    │                             #   toolbar management
    ├── ViewModels/               # Toolbar and Settings view models
    └── Views/                    # ToolbarWindow, SettingsWindow
tests/
├── SlipSnap.Tests/               # Unit tests (xUnit + FluentAssertions)
└── SlipSnap.E2ETests/            # End-to-end tests (FlaUI)
```

## Configuration

Settings are stored in `%AppData%\SlipSnap\settings.json` and can be edited via the Settings dialog (right-click the tray icon).

| Setting | Default | Description |
|---------|---------|-------------|
| Theme | Auto | Auto, Light, or Dark |
| Opacity | 80% | Toolbar transparency (10–100%) |
| Toolbars | Left edge enabled | Which edges have toolbars and which buttons each shows |
| Fullscreen Only | Off | Only show toolbars when a fullscreen window is present |
| Hide in RDP | On | Auto-hide toolbars when running inside a Remote Desktop session |

## Tech Stack

- **C# 13 / .NET 8** — `net8.0-windows10.0.22621.0`
- **WPF** — window management and XAML UI
- **WPF UI (wpfui)** — Windows 11 Fluent Design controls and theming
- **Slions.VirtualDesktop.WPF** — virtual desktop pinning and switching
- **Serilog** — structured diagnostic logging with rolling file sink
- **xUnit + FluentAssertions** — unit testing
- **FlaUI** — UI Automation end-to-end testing
