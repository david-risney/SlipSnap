# Data Model: SlipSnap WPF Floating Toolbar

**Feature**: 001-slipsnap-wpf-app  
**Date**: 2026-04-02

## Entities

### ToolbarEdge (Enum)

Identifies which monitor edge a toolbar is snapped to.

| Value | Description |
|-------|-------------|
| Left | Left edge — vertical layout |
| Right | Right edge — vertical layout |
| Top | Top edge — horizontal layout |
| Bottom | Bottom edge — horizontal layout |

### ToolbarButtonType (Enum)

Identifies the action a toolbar button performs.

| Value | Description | Keystroke |
|-------|-------------|-----------|
| StartMenu | Opens Windows Start Menu | `Win` key |
| TaskView | Opens Windows Task View | `Win+Tab` |
| NextDesktop | Switches to next virtual desktop | `Ctrl+Win+Right` |
| PrevDesktop | Switches to previous virtual desktop | `Ctrl+Win+Left` |

### ToolbarConfig

Configuration for a single toolbar instance.

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| Edge | ToolbarEdge | Which monitor edge | Left |
| IsEnabled | bool | Whether this toolbar is visible | true (for Left), false (others) |
| Buttons | List\<ToolbarButtonType\> | Which buttons appear, in order | All four button types |
| PositionPercent | double | Position along the edge axis (0.0 = start, 1.0 = end) | 0.5 (centered) |

**Validation rules**:
- `Buttons` must not be empty when `IsEnabled` is true.
- `PositionPercent` must be clamped to [0.0, 1.0].
- Duplicate `ToolbarButtonType` entries in `Buttons` are ignored (each button type appears at most once per toolbar).

### AppSettings (Root entity)

Complete application settings persisted to `%AppData%\SlipSnap\settings.json`.

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| Theme | ThemeMode | Color theme | Auto |
| OpacityPercent | int | Toolbar opacity (10–100) | 80 |
| Toolbars | Dictionary\<ToolbarEdge, ToolbarConfig\> | One config per edge (4 entries) | Left enabled, others disabled |
| FullscreenOnly | bool | Show toolbars only when fullscreen window detected | false |
| HideInRdpSession | bool | Hide toolbars when running inside RDP | true |

**Validation rules**:
- `OpacityPercent` clamped to [10, 100].
- `Toolbars` always has exactly 4 entries (one per edge). Missing entries are populated with defaults on load.
- If JSON is corrupt or missing, the app creates a fresh default settings file.

### ThemeMode (Enum)

| Value | Description |
|-------|-------------|
| Auto | Follow Windows system theme |
| Light | Force light theme |
| Dark | Force dark theme |

## Relationships

```text
AppSettings
├── Theme: ThemeMode
├── OpacityPercent: int
├── FullscreenOnly: bool
├── HideInRdpSession: bool
└── Toolbars: Dictionary<ToolbarEdge, ToolbarConfig>
    ├── [Left]  → ToolbarConfig { Edge, IsEnabled, Buttons[], PositionPercent }
    ├── [Right] → ToolbarConfig { ... }
    ├── [Top]   → ToolbarConfig { ... }
    └── [Bottom]→ ToolbarConfig { ... }
                    └── Buttons: List<ToolbarButtonType>
```

## State Transitions

### Toolbar Visibility State Machine

```text
                ┌──────────┐
                │  Hidden  │◄──── HideInRdpSession=true AND in RDP session
                └────┬─────┘      OR FullscreenOnly=true AND no fullscreen window
                     │             OR IsEnabled=false
                     │ (conditions cleared)
                     ▼
                ┌──────────┐
                │  Visible │──── Normal operating state
                └────┬─────┘
                     │
                     │ (user drags grip)
                     ▼
                ┌──────────┐
                │ Dragging │──── Position updating along edge axis
                └──────────┘
```

**Visibility evaluation order** (first match wins):
1. If `HideInRdpSession` is true AND running in RDP session → Hidden (tray icon still visible for settings access)
2. If `IsEnabled` is false for this edge → Hidden
3. If `FullscreenOnly` is true AND no fullscreen window detected → Hidden
4. Otherwise → Visible

## JSON Schema (settings.json)

```json
{
  "theme": "Auto",
  "opacityPercent": 80,
  "fullscreenOnly": false,
  "hideInRdpSession": true,
  "toolbars": {
    "Left": {
      "isEnabled": true,
      "buttons": ["StartMenu", "TaskView", "NextDesktop", "PrevDesktop"],
      "positionPercent": 0.5
    },
    "Right": {
      "isEnabled": false,
      "buttons": ["StartMenu", "TaskView", "NextDesktop", "PrevDesktop"],
      "positionPercent": 0.5
    },
    "Top": {
      "isEnabled": false,
      "buttons": ["StartMenu", "TaskView", "NextDesktop", "PrevDesktop"],
      "positionPercent": 0.5
    },
    "Bottom": {
      "isEnabled": false,
      "buttons": ["StartMenu", "TaskView", "NextDesktop", "PrevDesktop"],
      "positionPercent": 0.5
    }
  }
}
```
