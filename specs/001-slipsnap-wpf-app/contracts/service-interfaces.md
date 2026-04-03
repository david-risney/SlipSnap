# Service Contracts: SlipSnap WPF Floating Toolbar

**Feature**: 001-slipsnap-wpf-app  
**Date**: 2026-04-02

SlipSnap is a desktop application with no external API. The contracts below define the internal **service interfaces** that form the seam between business logic and platform-specific Win32/COM code. These interfaces enable unit testing without a live desktop session (Constitution I).

---

## ISettingsService

Manages loading, saving, and change notification for application settings.

```csharp
public interface ISettingsService
{
    /// <summary>Load settings from disk. Returns defaults if file missing or corrupt.</summary>
    AppSettings Load();

    /// <summary>Save current settings to disk. Throws IOException on write failure.</summary>
    void Save(AppSettings settings);

    /// <summary>Raised whenever settings change (from UI or external file edit).</summary>
    event EventHandler<AppSettings>? SettingsChanged;
}
```

**Behavior**:
- `Load()` returns a valid `AppSettings` even if the JSON file is missing, empty, or corrupt (logs the error, returns defaults).
- `Save()` writes atomically (write temp file, rename) to avoid corruption.
- File path: `%AppData%\SlipSnap\settings.json`.

---

## IKeyboardSimulator

Abstracts keyboard input injection for toolbar button actions.

```csharp
public interface IKeyboardSimulator
{
    /// <summary>Simulate pressing and releasing a key combination.</summary>
    void SendKeys(params VirtualKey[] keys);
}
```

**Concrete implementation** uses `keybd_event` P/Invoke. The `VirtualKey` enum wraps Win32 virtual key codes (VK_LWIN, VK_TAB, VK_LCONTROL, VK_LEFT, VK_RIGHT).

---

## IVirtualDesktopService

Abstracts virtual desktop operations (pinning, switching, availability check).

```csharp
public interface IVirtualDesktopService
{
    /// <summary>True if the virtual desktop API is available on this OS build.</summary>
    bool IsAvailable { get; }

    /// <summary>Pin a window to all virtual desktops. No-op if not available.</summary>
    void PinWindow(IntPtr hwnd);

    /// <summary>Switch to the next virtual desktop. No-op if on the last desktop.</summary>
    void SwitchToNext();

    /// <summary>Switch to the previous virtual desktop. No-op if on the first desktop.</summary>
    void SwitchToPrevious();
}
```

**Behavior**:
- `IsAvailable` performs a one-time check at construction. If COM interface not found, returns false.
- All methods log errors via `ILogger` and degrade gracefully (no exceptions thrown to callers).
- Concrete implementation wraps `Slions.VirtualDesktop.WPF`.

---

## IFullscreenDetector

Detects whether a fullscreen window is present on the monitor.

```csharp
public interface IFullscreenDetector
{
    /// <summary>True if any window covers the entire monitor area.</summary>
    bool IsFullscreenWindowPresent { get; }

    /// <summary>Raised when fullscreen state changes.</summary>
    event EventHandler<bool>? FullscreenStateChanged;

    /// <summary>Start monitoring for fullscreen window changes.</summary>
    void StartMonitoring();

    /// <summary>Stop monitoring and release hooks.</summary>
    void StopMonitoring();
}
```

**Behavior**:
- Uses `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` to detect foreground changes (event-driven, no polling).
- On each foreground change, enumerates top-level windows via `EnumWindows` and checks bounds against monitor area.
- `FullscreenStateChanged` fires only on transitions (true→false or false→true), not on every foreground change.

---

## IRdpSessionDetector

Detects whether the app is running inside a Remote Desktop session.

```csharp
public interface IRdpSessionDetector
{
    /// <summary>True if running inside an RDP session.</summary>
    bool IsRdpSession { get; }
}
```

**Behavior**:
- Calls `GetSystemMetrics(SM_REMOTESESSION)` once at construction.
- Optionally listens for `WM_WTSSESSION_CHANGE` to detect session transitions (connect/disconnect).

---

## IToolbarManager

Creates, positions, and manages lifecycle of toolbar windows.

```csharp
public interface IToolbarManager
{
    /// <summary>Apply settings: create/destroy/reposition toolbars to match config.</summary>
    void ApplySettings(AppSettings settings);

    /// <summary>Update visibility based on current fullscreen/RDP state.</summary>
    void UpdateVisibility(bool isFullscreenPresent, bool isRdpSession);

    /// <summary>Cleanly close all toolbar windows.</summary>
    void CloseAll();
}
```

**Behavior**:
- `ApplySettings()` diffs current toolbar state against new settings. Creates new toolbars, destroys removed ones, repositions moved ones. Does not recreate unchanged toolbars.
- `UpdateVisibility()` shows/hides toolbars based on the visibility state machine (see data-model.md).
- `CloseAll()` deterministically releases all HWNDs and event hooks (Constitution IV).
