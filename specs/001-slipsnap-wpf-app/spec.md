# Feature Specification: SlipSnap WPF Floating Toolbar

**Feature Branch**: `001-slipsnap-wpf-app`  
**Created**: 2026-04-02  
**Status**: Draft  
**Input**: User description: "Write a new WPF app named SlipSnap. It shows a semitransparent floating toolbar window snapped to the side of the monitor and on top of other windows including remote desktop client windows. It shows buttons to allow easy and quick access to, start menu, task view, next/prev virtual desktop and a grip to move the toolbar along the side of the monitor. It is displayed on all virtual desktops. The app icon in the taskbar provides actions to quit, and open settings. The settings dialog uses modern wpf library to give it a modern windows UI appearance. The settings dialog allows you to change the color theme from dark/light/auto (default to auto) and the percentage of transparency. It also lets you customize which icons appear in multiple toolbars. You can pick if toolbars should be on the left, right, top, and/or bottom of the monitor and for each of those which buttons should be displayed. And settings to not display the toolbars unless there is a fullscreen window (default unchecked). And settings to not display the toolbars when running from within an RDP session (Default checked). Tests for all features including end to end using a UIA and screenshot skill."

## Clarifications

### Session 2026-04-02

- Q: Should toolbar layout follow the edge orientation (vertical on left/right, horizontal on top/bottom)? → A: Yes — edge-adaptive layout: vertical stack on left/right, horizontal on top/bottom.
- Q: Where and how should user settings be stored? → A: JSON file in `%AppData%\SlipSnap\settings.json`.
- Q: What should the minimum toolbar opacity be? → A: 10% opacity (slider range 10%–100%).
- Q: How should the app detect a "fullscreen" window for fullscreen-only mode? → A: A window whose bounds cover the full monitor area (including borderless fullscreen like RDP client).
- Q: Should the app include diagnostic logging? → A: Yes — log to `%AppData%\SlipSnap\logs\` with basic rotation.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Floating Toolbar Over Fullscreen RDP (Priority: P1)

A user is working in a fullscreen Remote Desktop session and needs to switch to a different virtual desktop on the host machine. They move their mouse to the edge of the screen where the SlipSnap toolbar is snapped. The toolbar is visible above the RDP window. They click the "Next Desktop" button and the host machine switches to the next virtual desktop — without the toolbar stealing focus from RDP.

**Why this priority**: This is the core value proposition — providing accessible controls on top of fullscreen apps that capture all input. Without this, the app has no reason to exist.

**Independent Test**: Launch SlipSnap, open a maximized or fullscreen window, verify the toolbar renders above it. Click a virtual desktop button and verify the desktop switches. Automated via UI Automation to verify toolbar element visibility and button invocability, plus screenshot comparison to confirm Z-order above the target window.

**Acceptance Scenarios**:

1. **Given** SlipSnap is running and a fullscreen window is active, **When** the user looks at the toolbar edge, **Then** the semi-transparent toolbar is visible above the fullscreen window.
2. **Given** the toolbar is visible over a fullscreen window, **When** the user clicks "Next Desktop", **Then** the host switches to the next virtual desktop and the fullscreen app retains focus.
3. **Given** the toolbar is visible over a fullscreen window, **When** the user clicks "Previous Desktop", **Then** the host switches to the previous virtual desktop and the fullscreen app retains focus.
4. **Given** SlipSnap is running, **When** the user switches to any virtual desktop, **Then** the toolbar is visible on every desktop (pinned to all desktops).

---

### User Story 2 - Start Menu and Task View Access (Priority: P1)

A user working in a fullscreen application cannot access the Start Menu or Task View using their mouse. They click the Start Menu button on the SlipSnap toolbar and the Windows Start Menu opens. Similarly, they can click the Task View button to see all open windows.

**Why this priority**: Start Menu and Task View are fundamental OS navigation actions that are inaccessible when a fullscreen app captures input. Equal priority to virtual desktop switching.

**Independent Test**: Launch SlipSnap, click the Start Menu button, verify the Start Menu opens. Click the Task View button, verify Task View opens. Automated via UIA button invocation and screenshot verification of the Start Menu / Task View appearing.

**Acceptance Scenarios**:

1. **Given** SlipSnap is running, **When** the user clicks the Start Menu button, **Then** the Windows Start Menu opens.
2. **Given** SlipSnap is running, **When** the user clicks the Task View button, **Then** Windows Task View opens.

---

### User Story 3 - Toolbar Positioning and Grip Drag (Priority: P2)

A user wants the toolbar on the left edge of the monitor but positioned closer to the bottom. They grab the drag grip on the toolbar and slide it along the edge to reposition it. The toolbar stays snapped to the edge but moves along it.

**Why this priority**: Positioning flexibility is important for ergonomics and different monitor setups, but the app is usable with a sensible default position.

**Independent Test**: Launch SlipSnap, use the grip to drag the toolbar along the monitor edge, verify it stays snapped to the edge and remembers its position after restart. Automated via UIA drag operations and screenshot comparison of toolbar position before and after drag.

**Acceptance Scenarios**:

1. **Given** a toolbar is displayed on a monitor edge, **When** the user clicks and drags the grip, **Then** the toolbar moves along that edge following the mouse.
2. **Given** the user drags the toolbar to a new position, **When** the user releases the grip, **Then** the toolbar stays at the new position.
3. **Given** the user repositioned a toolbar, **When** SlipSnap is restarted, **Then** the toolbar reappears at the last saved position.

---

### User Story 4 - Settings Dialog with Modern Appearance (Priority: P2)

A user right-clicks the SlipSnap tray icon and opens Settings. A modern-looking dialog appears matching Windows 11 aesthetics. They switch the theme to dark mode, reduce transparency to 60%, and add a toolbar on the right edge. They close settings and the changes apply immediately.

**Why this priority**: Customization makes the app adaptable to user preferences and multi-monitor setups. The settings dialog is the primary configuration surface.

**Independent Test**: Launch SlipSnap, open the Settings dialog, change theme, transparency, and toolbar layout. Verify each change takes effect. Automated via UIA navigation of settings controls and screenshot comparison of before/after theme changes.

**Acceptance Scenarios**:

1. **Given** SlipSnap is running, **When** the user right-clicks the taskbar icon and selects "Settings", **Then** the settings dialog opens with a modern Windows UI appearance.
2. **Given** the settings dialog is open, **When** the user changes the theme to "Dark", **Then** the toolbar and settings dialog switch to dark mode.
3. **Given** the settings dialog is open, **When** the user changes transparency to 60%, **Then** the toolbar opacity updates to 60% in real-time.
4. **Given** the settings dialog is open, **When** the user enables a toolbar on the right edge and selects which buttons appear, **Then** a new toolbar with those buttons appears on the right edge of the monitor.
5. **Given** settings have been changed, **When** SlipSnap is restarted, **Then** all settings persist and are restored.

---

### User Story 5 - Multiple Toolbar Configuration (Priority: P3)

A user wants different buttons on different edges. They open Settings and configure a left-edge toolbar with Start Menu and Task View buttons, and a bottom-edge toolbar with virtual desktop navigation buttons. Each toolbar shows only its assigned buttons.

**Why this priority**: Multi-toolbar configuration is an advanced use case. Most users will use a single toolbar. Value comes from power-user workflows.

**Independent Test**: Open Settings, configure two toolbars on different edges with different button sets. Verify each toolbar shows the correct buttons. Automated via UIA enumeration of buttons per toolbar window.

**Acceptance Scenarios**:

1. **Given** the user enables toolbars on left and bottom edges, **When** they assign Start Menu to left and Desktop Nav to bottom, **Then** two distinct toolbars appear with the correct buttons.
2. **Given** multiple toolbars are configured, **When** one toolbar is disabled in settings, **Then** only that toolbar disappears and the other remains.
3. **Given** multiple toolbars are configured, **When** the user removes a button from one toolbar, **Then** only that toolbar's button set changes.

---

### User Story 6 - Fullscreen-Only Mode (Priority: P3)

A user wants toolbars to appear only when a fullscreen window is detected. They enable "Only show toolbars when a fullscreen window is present" in Settings. The toolbars hide when no fullscreen window exists and reappear when one becomes active.

**Why this priority**: This is a visibility refinement — reduces desktop clutter for users who only need SlipSnap in fullscreen scenarios.

**Independent Test**: Enable fullscreen-only mode, maximize a window to fullscreen, verify toolbar appears. Minimize/close the fullscreen window, verify toolbar hides. Automated via UIA visibility checks and window state manipulation.

**Acceptance Scenarios**:

1. **Given** fullscreen-only mode is enabled and no fullscreen window is present, **When** the user looks at the toolbar edge, **Then** the toolbar is hidden.
2. **Given** fullscreen-only mode is enabled, **When** a window becomes fullscreen, **Then** the toolbar becomes visible.
3. **Given** fullscreen-only mode is enabled and a fullscreen window is present, **When** the fullscreen window is closed or minimized, **Then** the toolbar hides again.

---

### User Story 7 - RDP Session Detection (Priority: P3)

A user remotes into a machine via RDP. SlipSnap is installed on both the host and the remote machine. By default, the remote instance hides its toolbars (since the host instance is the one that matters). The user can override this in settings if desired.

**Why this priority**: Prevents confusing double-toolbars when SlipSnap runs on both host and remote. Good default behavior for advanced users.

**Independent Test**: Launch SlipSnap in an RDP session with default settings, verify toolbars are hidden. Change the setting to show in RDP, verify toolbars appear. Automated via programmatic RDP session detection and UIA visibility checks.

**Acceptance Scenarios**:

1. **Given** SlipSnap is running inside an RDP session with default settings, **When** the app starts, **Then** the toolbars are hidden.
2. **Given** SlipSnap is running inside an RDP session, **When** the user unchecks "Hide toolbars in RDP session" in settings, **Then** the toolbars become visible.
3. **Given** SlipSnap is running outside an RDP session, **When** the app starts, **Then** the toolbars are visible regardless of the RDP setting.

---

### User Story 8 - Taskbar Icon with Quit and Settings (Priority: P2)

A user wants to quit SlipSnap or open settings. They right-click the SlipSnap icon in the system tray / taskbar notification area. A context menu appears with "Settings" and "Quit" options.

**Why this priority**: Users need a discoverable way to access settings and exit the app. The taskbar icon is the standard Windows pattern for background apps.

**Independent Test**: Launch SlipSnap, verify the taskbar icon appears. Right-click the icon, verify the context menu shows. Click Quit, verify the app exits. Automated via UIA interaction with the notification area icon.

**Acceptance Scenarios**:

1. **Given** SlipSnap is running, **When** the user looks at the system tray, **Then** a SlipSnap icon is present.
2. **Given** the user right-clicks the tray icon, **When** they select "Settings", **Then** the settings dialog opens.
3. **Given** the user right-clicks the tray icon, **When** they select "Quit", **Then** SlipSnap exits cleanly and all toolbars disappear.

---

### Edge Cases

- What happens when the monitor resolution changes while toolbars are displayed? Toolbars must reposition to stay within screen bounds.
- What happens when a monitor is disconnected that had a toolbar? The toolbar must close or migrate to a remaining monitor.
- What happens when virtual desktop APIs are unavailable (older Windows version, COM interface changed)? Desktop switching buttons must be disabled gracefully with a tooltip explaining why.
- What happens when the user sets transparency to 0%? The slider enforces a minimum of 10% opacity — the user cannot go below that.
- What happens when SlipSnap is launched without UIAccess (unsigned exe, wrong directory)? The toolbar should still work but may not appear above fullscreen apps. A one-time notification should inform the user.
- What happens when multiple monitors are connected? Each monitor may have independent toolbar configurations, or toolbars only appear on the primary monitor. (Assumption: single-monitor support for v1.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST display semi-transparent toolbar windows snapped to the edges of the monitor.
- **FR-002**: The toolbars MUST render above all other windows, including fullscreen Remote Desktop windows, using UIAccess elevation.
- **FR-003**: The toolbar MUST include buttons for: Start Menu, Task View, Next Virtual Desktop, and Previous Virtual Desktop.
- **FR-004**: Each toolbar MUST include a grip element that allows the user to drag the toolbar along the edge it is snapped to. Toolbars on left/right edges MUST stack buttons vertically with a vertical grip; toolbars on top/bottom edges MUST stack buttons horizontally with a horizontal grip.
- **FR-005**: The toolbars MUST be pinned to all virtual desktops so they are visible regardless of which desktop is active.
- **FR-006**: The app MUST display a system tray icon with a context menu offering "Settings" and "Quit" actions.
- **FR-007**: The settings dialog MUST support a color theme selector with Dark, Light, and Auto options, defaulting to Auto.
- **FR-008**: The settings dialog MUST support a transparency percentage slider that controls toolbar opacity.
- **FR-009**: The settings dialog MUST allow the user to enable or disable toolbars on each of the four monitor edges (left, right, top, bottom).
- **FR-010**: The settings dialog MUST allow the user to choose which buttons appear on each enabled toolbar independently.
- **FR-011**: The app MUST persist all user settings across sessions in a JSON file at `%AppData%\SlipSnap\settings.json`.
- **FR-012**: The settings dialog MUST have a modern Windows 11 visual appearance using a modern WPF UI library.
- **FR-013**: The app MUST support a "fullscreen-only" mode that hides toolbars when no fullscreen window is detected (default: unchecked / toolbars always visible). A window is considered fullscreen when its bounds cover the entire monitor area, which includes both exclusive fullscreen and borderless fullscreen windows such as the Remote Desktop client.
- **FR-014**: The app MUST detect when running inside an RDP session and hide toolbars by default, with a setting to override (default: checked / hidden in RDP).
- **FR-015**: Virtual desktop buttons MUST NOT steal focus from the currently active fullscreen application.
- **FR-016**: The app MUST have comprehensive automated tests covering all features, including end-to-end tests using UI Automation and screenshot verification.
- **FR-017**: Theme changes, transparency changes, and toolbar layout changes MUST take effect immediately without requiring a restart.
- **FR-018**: The app MUST enforce a minimum opacity of 10% so users cannot make the toolbar completely invisible. The transparency slider range is 10%–100%.
- **FR-019**: The app MUST log diagnostic events (startup, UIAccess status, COM failures, fullscreen detection, settings load/save errors) to `%AppData%\SlipSnap\logs\` with basic file rotation.

### Key Entities

- **Toolbar**: A floating window snapped to a monitor edge. Has an edge position (left/right/top/bottom), a layout direction (vertical for left/right edges, horizontal for top/bottom edges), a list of enabled buttons, and a grip for repositioning along the edge axis. Multiple toolbars may exist simultaneously.
- **Toolbar Button**: An action trigger displayed in a toolbar. Types: Start Menu, Task View, Next Desktop, Previous Desktop. Each button can be independently assigned to any toolbar.
- **Settings Profile**: The complete set of user preferences: theme (dark/light/auto), transparency percentage, toolbar configurations (edge + buttons + position per toolbar), fullscreen-only toggle, and hide-in-RDP toggle. Stored as a JSON file in `%AppData%\SlipSnap\settings.json`.
- **Tray Icon**: The notification area icon providing the context menu for Settings and Quit.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can invoke Start Menu, Task View, or switch virtual desktops via toolbar buttons within 1 second of clicking, even when a fullscreen application is in the foreground.
- **SC-002**: The toolbar remains visible above fullscreen Remote Desktop windows 100% of the time when UIAccess is active.
- **SC-003**: All user-configurable settings (theme, transparency, toolbar layout, fullscreen-only, RDP detection) take effect within 1 second of changing, without app restart.
- **SC-004**: The app starts and displays toolbars within 2 seconds of launch on target hardware.
- **SC-005**: 100% of functional requirements are covered by automated tests, with end-to-end tests using UI Automation that verify toolbar visibility, button actions, drag-to-reposition, settings changes, and tray icon interactions.
- **SC-006**: Screenshot-based tests confirm visual correctness of toolbar rendering, theme changes, and transparency adjustments.
- **SC-007**: The app uses less than 50 MB of memory at steady state with up to 4 toolbars active.

## Assumptions

- Target platform is Windows 10 22H2+ and Windows 11. No macOS or Linux support.
- Single-monitor support for the initial version. Multi-monitor toolbar placement is out of scope for v1.
- The app requires UIAccess for its core value (staying on top of fullscreen windows). A graceful degradation mode exists but is not the primary supported scenario.
- Virtual desktop APIs use the `Slions.VirtualDesktop.WPF` NuGet package (or similar), which relies on undocumented COM interfaces that may break between Windows builds.
- MSIX packaging is not viable due to UIAccess incompatibility (per prototype research). Deployment is via signed exe to `C:\Program Files\`.
- A self-signed certificate is acceptable for development; production distribution requires a code-signing certificate from a trusted CA.
- "Modern WPF UI library" means a library such as WPF UI (wpfui) or similar that provides Windows 11 Fluent Design controls.
- The prototype in the `prototype/` folder provides validated research for: UIAccess behavior, Z-order strategy, host vs remote keystroke injection, window non-activation patterns, and virtual desktop pinning.
