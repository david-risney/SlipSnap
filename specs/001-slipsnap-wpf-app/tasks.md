# Tasks: SlipSnap WPF Floating Toolbar

**Input**: Design documents from `/specs/001-slipsnap-wpf-app/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — FR-016 explicitly requires comprehensive automated tests including E2E with UIA and screenshot verification.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1–US8)
- All file paths are relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, solution structure, NuGet packages

- [X] T001 Create solution and project structure per quickstart.md: `dotnet new sln`, `dotnet new wpf` for `src/SlipSnap/`, `dotnet new xunit` for `tests/SlipSnap.Tests/` and `tests/SlipSnap.E2ETests/`, add project references
- [X] T002 Add NuGet packages: WPF-UI 4.2.0, Slions.VirtualDesktop.WPF 6.9.2, Serilog.Sinks.File, Serilog.Extensions.Logging to `src/SlipSnap/SlipSnap.csproj`; FluentAssertions to both test projects; FlaUI.UIA3 5.0.0 to `tests/SlipSnap.E2ETests/`
- [X] T003 Configure `src/SlipSnap/SlipSnap.csproj` with `net8.0-windows10.0.22621.0`, `UseWPF`, `Nullable enable`, `TreatWarningsAsErrors`, `ApplicationManifest=app.manifest`
- [X] T004 [P] Create `src/SlipSnap/app.manifest` with `uiAccess="true"` and `level="highestAvailable"` per prototype research
- [X] T005 [P] Create `.editorconfig` at repository root with C# coding conventions (Constitution II)
- [X] T006 [P] Create `.gitignore` entries for new `src/`, `tests/`, `bin/`, `obj/`, `publish/` directories

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models, interop, services interfaces, logging — MUST complete before any user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 [P] Create `ToolbarEdge` enum in `src/SlipSnap/Models/ToolbarEdge.cs` per data-model.md
- [X] T008 [P] Create `ToolbarButtonType` enum in `src/SlipSnap/Models/ToolbarButtonType.cs` per data-model.md
- [X] T009 [P] Create `ThemeMode` enum in `src/SlipSnap/Models/ThemeMode.cs` per data-model.md
- [X] T010 [P] Create `ToolbarConfig` model class in `src/SlipSnap/Models/ToolbarConfig.cs` with validation rules per data-model.md
- [X] T011 Create `AppSettings` root model class in `src/SlipSnap/Models/AppSettings.cs` with defaults and validation per data-model.md (depends on T007–T010)
- [X] T012 [P] Create `NativeMethods` static class in `src/SlipSnap/Interop/NativeMethods.cs` with all P/Invoke declarations: `keybd_event`, `SetForegroundWindow`, `GetForegroundWindow`, `EnumWindows`, `GetWindowRect`, `MonitorFromWindow`, `GetMonitorInfo`, `SetWinEventHook`, `UnhookWinEvent`, `GetSystemMetrics`, and Win32 constants (`SM_REMOTESESSION`, `EVENT_SYSTEM_FOREGROUND`, `VK_*`, `WS_EX_*`, `MA_NOACTIVATE`)
- [X] T013 [P] Create `VirtualKey` enum in `src/SlipSnap/Interop/NativeMethods.cs` (or separate file) wrapping Win32 VK codes used by toolbar buttons
- [X] T014 [P] Create all service interfaces per contracts/service-interfaces.md: `ISettingsService` in `src/SlipSnap/Services/ISettingsService.cs`, `IKeyboardSimulator` in `src/SlipSnap/Services/IKeyboardSimulator.cs`, `IVirtualDesktopService` in `src/SlipSnap/Services/IVirtualDesktopService.cs`, `IFullscreenDetector` in `src/SlipSnap/Services/IFullscreenDetector.cs`, `IRdpSessionDetector` in `src/SlipSnap/Services/IRdpSessionDetector.cs`, `IToolbarManager` in `src/SlipSnap/Services/IToolbarManager.cs`
- [X] T015 Implement `SettingsService` in `src/SlipSnap/Services/SettingsService.cs`: JSON load/save with atomic write (temp file + rename), default generation on missing/corrupt file, `%AppData%\SlipSnap\` directory creation (depends on T011, T014)
- [X] T016 Configure Serilog logging in `src/SlipSnap/App.xaml.cs`: rolling file sink to `%AppData%\SlipSnap\logs\`, wire into `Microsoft.Extensions.Logging.ILogger`
- [X] T017 Set up WPF UI theming in `src/SlipSnap/App.xaml`: add `ui:ThemesDictionary` and `ui:ControlsDictionary` resource dictionaries, configure `ApplicationThemeManager` for Auto/Light/Dark switching
- [X] T018 [P] Write unit tests for `AppSettings` validation (clamping, defaults) in `tests/SlipSnap.Tests/Models/AppSettingsTests.cs`
- [X] T019 [P] Write unit tests for `SettingsService` (load defaults, save/load roundtrip, corrupt file recovery) in `tests/SlipSnap.Tests/Services/SettingsServiceTests.cs`

**Checkpoint**: Foundation ready — all models, interfaces, settings persistence, logging, and theming in place

---

## Phase 3: User Story 1 — Floating Toolbar Over Fullscreen RDP (Priority: P1) 🎯 MVP

**Goal**: Display a semi-transparent toolbar window on the left edge of the monitor, above all windows including fullscreen RDP. Buttons for Next/Previous virtual desktop switch the host desktop without stealing focus. Toolbar pinned to all virtual desktops.

**Independent Test**: Launch app → toolbar visible on left edge → click Next Desktop → host desktop switches → fullscreen app retains focus.

### Tests for User Story 1

- [X] T020 [P] [US1] Write unit tests for `ToolbarViewModel` (button commands invoke correct service methods, non-activation flag) in `tests/SlipSnap.Tests/ViewModels/ToolbarViewModelTests.cs`
- [X] T021 [P] [US1] Write unit tests for `KeyboardSimulator` contract (correct VK sequences for each button type) in `tests/SlipSnap.Tests/Services/KeyboardSimulatorTests.cs`
- [X] T022 [P] [US1] Write E2E test: launch app, verify toolbar window exists via UIA, verify it has Next/Prev Desktop buttons in `tests/SlipSnap.E2ETests/ToolbarVisibilityTests.cs`
- [X] T023 [P] [US1] Write E2E helper `AppLauncher` in `tests/SlipSnap.E2ETests/Helpers/AppLauncher.cs` to start/stop SlipSnap process for tests

### Implementation for User Story 1

- [X] T024 [US1] Implement `KeyboardSimulator` in `src/SlipSnap/Services/KeyboardSimulator.cs` using `keybd_event` from `NativeMethods` for Ctrl+Win+Left/Right keystroke injection (depends on T012, T014)
- [X] T025 [US1] Implement `VirtualDesktopService` in `src/SlipSnap/Services/VirtualDesktopService.cs` wrapping `Slions.VirtualDesktop.WPF`: `IsAvailable` check, `PinWindow()`, `SwitchToNext()`, `SwitchToPrevious()` with graceful COM error handling and logging (depends on T014)
- [X] T026 [US1] Create `ToolbarViewModel` in `src/SlipSnap/ViewModels/ToolbarViewModel.cs` with `ICommand` properties for NextDesktop, PrevDesktop; wire to `IKeyboardSimulator` and `IVirtualDesktopService`; implement conditional `_blockActivation` flag per prototype research (depends on T024, T025)
- [X] T027 [US1] Create `ToolbarWindow.xaml` in `src/SlipSnap/Views/ToolbarWindow.xaml` — transparent, borderless, topmost WPF window with `AllowsTransparency=True`, `ShowInTaskbar=False`, `WindowStyle=None`; vertical button stack with semi-transparent background; NextDesktop and PrevDesktop buttons using WPF UI controls
- [X] T028 [US1] Implement `ToolbarWindow.xaml.cs` code-behind: `WndProc` hook for `WM_MOUSEACTIVATE` → conditional `MA_NOACTIVATE`; window snapping to left edge; `SetForegroundWindow` handling for host buttons; apply opacity from settings (depends on T027, T026)
- [X] T029 [US1] Implement `ToolbarManager` (partial) in `src/SlipSnap/Services/ToolbarManager.cs`: `ApplySettings()` creates a single left-edge toolbar, `CloseAll()` disposes windows, handles virtual desktop pinning via `IVirtualDesktopService.PinWindow()` (depends on T025, T028)
- [X] T030 [US1] Wire up `App.xaml.cs` startup: load settings, create `ToolbarManager`, call `ApplySettings()`, pin toolbar to all desktops (depends on T015, T016, T017, T029)

**Checkpoint**: US1 complete — toolbar visible on left edge, virtual desktop switching works, pinned to all desktops

---

## Phase 4: User Story 2 — Start Menu and Task View Access (Priority: P1)

**Goal**: Add Start Menu and Task View buttons to the toolbar. Clicking them invokes the corresponding OS action.

**Independent Test**: Launch app → click Start Menu button → Start Menu opens. Click Task View button → Task View opens.

### Tests for User Story 2

- [X] T031 [P] [US2] Write unit tests for Start Menu and Task View commands in `ToolbarViewModel` (correct VK sequences: Win, Win+Tab) in `tests/SlipSnap.Tests/ViewModels/ToolbarViewModelTests.cs` (append to existing file)
- [X] T032 [P] [US2] Write E2E test: click Start Menu button via UIA, verify Start Menu opens; click Task View button, verify Task View opens in `tests/SlipSnap.E2ETests/ToolbarButtonTests.cs`

### Implementation for User Story 2

- [X] T033 [US2] Extend `ToolbarViewModel` with `StartMenuCommand` and `TaskViewCommand` ICommand properties; wire `IKeyboardSimulator.SendKeys(VK_LWIN)` for Start Menu and `SendKeys(VK_LWIN, VK_TAB)` for Task View in `src/SlipSnap/ViewModels/ToolbarViewModel.cs`
- [X] T034 [US2] Add Start Menu (⌂) and Task View (☰) buttons to `ToolbarWindow.xaml` in `src/SlipSnap/Views/ToolbarWindow.xaml`, bind to new commands

**Checkpoint**: US2 complete — all 4 buttons functional (Start Menu, Task View, Next Desktop, Prev Desktop)

---

## Phase 5: User Story 8 — Taskbar Icon with Quit and Settings (Priority: P2)

**Goal**: System tray icon with context menu for Settings and Quit. Quit cleanly exits the app.

**Independent Test**: Launch app → tray icon appears → right-click → "Quit" exits cleanly.

### Tests for User Story 8

- [X] T035 [P] [US8] Write E2E test: verify tray icon exists via UIA; right-click and select Quit; verify process exits in `tests/SlipSnap.E2ETests/TrayIconTests.cs`

### Implementation for User Story 8

- [X] T036 [US8] Add WPF UI `TrayIcon` to `src/SlipSnap/App.xaml` with SlipSnap icon, context menu with "Settings" and "Quit" menu items
- [X] T037 [US8] Implement tray icon event handlers in `src/SlipSnap/App.xaml.cs`: "Quit" calls `ToolbarManager.CloseAll()` then `Application.Current.Shutdown()`; "Settings" opens SettingsWindow (placeholder for now)

**Checkpoint**: US8 complete — tray icon with working Quit action, Settings placeholder

---

## Phase 6: User Story 3 — Toolbar Positioning and Grip Drag (Priority: P2)

**Goal**: Add a grip element to the toolbar. User can drag the toolbar along the edge. Position persists across restarts.

**Independent Test**: Launch app → drag grip along edge → release → restart app → toolbar at saved position.

### Tests for User Story 3

- [X] T038 [P] [US3] Write unit tests for position clamping logic (0.0–1.0 range) and position-to-pixel conversion per edge in `tests/SlipSnap.Tests/ViewModels/ToolbarViewModelTests.cs`
- [X] T039 [P] [US3] Write E2E test: find grip via UIA, simulate drag, verify toolbar moved, restart app, verify position restored in `tests/SlipSnap.E2ETests/ToolbarDragTests.cs`

### Implementation for User Story 3

- [X] T040 [US3] Add grip element to `ToolbarWindow.xaml` in `src/SlipSnap/Views/ToolbarWindow.xaml` — drag handle above/below the button stack (vertical toolbar) or left/right (horizontal toolbar)
- [X] T041 [US3] Implement grip drag logic in `ToolbarWindow.xaml.cs`: `MouseDown`/`MouseMove`/`MouseUp` handlers that constrain movement to the snapped edge axis, convert pixel position to `PositionPercent`, save to settings via `ISettingsService` on drag end
- [X] T042 [US3] Update `ToolbarManager.ApplySettings()` in `src/SlipSnap/Services/ToolbarManager.cs` to position toolbar at the stored `PositionPercent` along the edge

**Checkpoint**: US3 complete — grip drag repositioning with persistence

---

## Phase 7: User Story 4 — Settings Dialog with Modern Appearance (Priority: P2)

**Goal**: Modern Fluent Design settings dialog with theme selector, transparency slider, and toolbar enable/disable per edge. Changes apply immediately.

**Independent Test**: Open Settings → change theme to Dark → toolbar switches to dark. Change transparency → toolbar opacity updates live.

### Tests for User Story 4

- [X] T043 [P] [US4] Write unit tests for `SettingsViewModel` (theme change notifies, opacity clamped 10–100, toolbar enable/disable toggles, save triggers) in `tests/SlipSnap.Tests/ViewModels/SettingsViewModelTests.cs`
- [X] T044 [P] [US4] Write E2E test: open Settings via tray icon, change theme, verify toolbar theme changes; change opacity slider, verify toolbar opacity in `tests/SlipSnap.E2ETests/SettingsDialogTests.cs`
- [X] T045 [P] [US4] Write screenshot test: capture toolbar before/after theme change, compare in `tests/SlipSnap.E2ETests/ScreenshotTests.cs`
- [X] T046 [P] [US4] Create `ScreenshotComparer` helper in `tests/SlipSnap.E2ETests/Helpers/ScreenshotComparer.cs` — capture via FlaUI, per-pixel diff with configurable tolerance threshold

### Implementation for User Story 4

- [X] T047 [US4] Create `SettingsViewModel` in `src/SlipSnap/ViewModels/SettingsViewModel.cs` — binds to `AppSettings`, exposes `ThemeMode` selector, `OpacityPercent` slider (10–100), per-edge `IsEnabled` toggles; calls `ISettingsService.Save()` on change; raises `PropertyChanged` for live updates (depends on T014, T015)
- [X] T048 [US4] Create `SettingsWindow.xaml` in `src/SlipSnap/Views/SettingsWindow.xaml` using WPF UI `FluentWindow` — theme radio buttons (Dark/Light/Auto), opacity slider with percentage label, four edge toggle sections (Left/Right/Top/Bottom) with enable checkbox
- [X] T049 [US4] Implement `SettingsWindow.xaml.cs` code-behind: bind to `SettingsViewModel`, apply `ApplicationThemeManager` on theme change, wire close button
- [X] T050 [US4] Connect tray icon "Settings" menu item to open `SettingsWindow` in `src/SlipSnap/App.xaml.cs` (replace placeholder from T037)
- [X] T051 [US4] Subscribe `ToolbarManager` to `ISettingsService.SettingsChanged` event in `src/SlipSnap/Services/ToolbarManager.cs` — on change, call `ApplySettings()` to update opacity, theme, and toolbar visibility live (FR-017)

**Checkpoint**: US4 complete — settings dialog with theme, transparency, and toolbar toggles; changes apply immediately

---

## Phase 8: User Story 5 — Multiple Toolbar Configuration (Priority: P3)

**Goal**: Enable toolbars on multiple edges simultaneously with independent button sets per edge.

**Independent Test**: Enable left + bottom toolbars with different buttons → verify two distinct toolbar windows with correct buttons.

### Tests for User Story 5

- [X] T052 [P] [US5] Write unit tests for `ToolbarManager` multi-toolbar logic (create/destroy/diff on settings change) in `tests/SlipSnap.Tests/Services/ToolbarManagerTests.cs`
- [X] T053 [P] [US5] Write E2E test: enable two toolbars on different edges, verify two windows via UIA, each with correct buttons in `tests/SlipSnap.E2ETests/ToolbarVisibilityTests.cs` (append)

### Implementation for User Story 5

- [X] T054 [US5] Extend `ToolbarWindow.xaml` to support edge-adaptive layout: vertical stack for Left/Right, horizontal stack for Top/Bottom; grip orientation follows edge in `src/SlipSnap/Views/ToolbarWindow.xaml`
- [X] T055 [US5] Extend `ToolbarWindow.xaml.cs` with edge-snapping logic for all four edges (left: x=0, right: x=screenWidth-toolbarWidth, top: y=0, bottom: y=screenHeight-toolbarHeight) in `src/SlipSnap/Views/ToolbarWindow.xaml.cs`
- [X] T056 [US5] Extend `ToolbarManager.ApplySettings()` to handle multiple simultaneous toolbars: diff current windows vs settings, create new, destroy removed, reposition moved in `src/SlipSnap/Services/ToolbarManager.cs`
- [X] T057 [US5] Add per-edge button selection UI to `SettingsWindow.xaml` — for each enabled edge, show checkboxes for StartMenu, TaskView, NextDesktop, PrevDesktop in `src/SlipSnap/Views/SettingsWindow.xaml`
- [X] T058 [US5] Update `SettingsViewModel` to handle per-edge button lists and notify `ToolbarManager` on changes in `src/SlipSnap/ViewModels/SettingsViewModel.cs`

**Checkpoint**: US5 complete — up to 4 toolbars on different edges with independent button sets

---

## Phase 9: User Story 6 — Fullscreen-Only Mode (Priority: P3)

**Goal**: Toolbars hide when no fullscreen window is present (when option enabled). Event-driven detection, no polling.

**Independent Test**: Enable fullscreen-only mode → no fullscreen window → toolbars hidden → maximize window to fullscreen → toolbars appear.

### Tests for User Story 6

- [X] T059 [P] [US6] Write unit tests for `FullscreenDetector` (mock `EnumWindows` result, verify state transitions fire only on change) in `tests/SlipSnap.Tests/Services/FullscreenDetectorTests.cs`
- [X] T060 [P] [US6] Write unit tests for `ToolbarManager.UpdateVisibility()` with fullscreen-only mode in `tests/SlipSnap.Tests/Services/ToolbarManagerTests.cs` (append)
- [X] T061 [P] [US6] Write E2E test: enable fullscreen-only mode, maximize a window, verify toolbar visible; restore window, verify toolbar hidden in `tests/SlipSnap.E2ETests/ToolbarVisibilityTests.cs` (append)

### Implementation for User Story 6

- [X] T062 [US6] Implement `FullscreenDetector` in `src/SlipSnap/Services/FullscreenDetector.cs` — `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`, `EnumWindows` + `GetWindowRect` + `GetMonitorInfo` to detect fullscreen windows; fire `FullscreenStateChanged` on transitions only (depends on T012, T014)
- [X] T063 [US6] Wire `FullscreenDetector` into `ToolbarManager.UpdateVisibility()` in `src/SlipSnap/Services/ToolbarManager.cs` — subscribe to `FullscreenStateChanged`, show/hide toolbars per visibility state machine from data-model.md
- [X] T064 [US6] Add "Only show when fullscreen window present" checkbox to `SettingsWindow.xaml` and bind to `SettingsViewModel.FullscreenOnly` in `src/SlipSnap/Views/SettingsWindow.xaml`
- [X] T065 [US6] Start/stop `FullscreenDetector` monitoring in `App.xaml.cs` startup/shutdown; ensure deterministic hook cleanup (Constitution IV) in `src/SlipSnap/App.xaml.cs`

**Checkpoint**: US6 complete — toolbars appear/disappear based on fullscreen state, event-driven

---

## Phase 10: User Story 7 — RDP Session Detection (Priority: P3)

**Goal**: Auto-hide toolbars when running inside an RDP session. Setting to override.

**Independent Test**: Launch inside RDP → toolbars hidden by default. Uncheck setting → toolbars appear.

### Tests for User Story 7

- [X] T066 [P] [US7] Write unit tests for `RdpSessionDetector` (mock `GetSystemMetrics` return value) in `tests/SlipSnap.Tests/Services/RdpSessionDetectorTests.cs`
- [X] T067 [P] [US7] Write unit tests for `ToolbarManager.UpdateVisibility()` with RDP detection in `tests/SlipSnap.Tests/Services/ToolbarManagerTests.cs` (append)

### Implementation for User Story 7

- [X] T068 [US7] Implement `RdpSessionDetector` in `src/SlipSnap/Services/RdpSessionDetector.cs` — call `GetSystemMetrics(SM_REMOTESESSION)` at construction (depends on T012, T014)
- [X] T069 [US7] Wire `RdpSessionDetector` into `ToolbarManager.UpdateVisibility()` — check `HideInRdpSession` setting + `IsRdpSession` per visibility state machine in `src/SlipSnap/Services/ToolbarManager.cs`
- [X] T070 [US7] Add "Hide toolbars in RDP session" checkbox (default checked) to `SettingsWindow.xaml` and bind to `SettingsViewModel.HideInRdpSession` in `src/SlipSnap/Views/SettingsWindow.xaml`
- [X] T071 [US7] Wire `IRdpSessionDetector` into `App.xaml.cs` startup, pass to `ToolbarManager` for initial visibility evaluation in `src/SlipSnap/App.xaml.cs`

**Checkpoint**: US7 complete — toolbars hidden in RDP sessions by default, setting to override

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, visual polish, comprehensive E2E coverage, quickstart validation

- [X] T072 [P] Handle monitor resolution change: subscribe to `SystemEvents.DisplaySettingsChanged`, reposition toolbars within screen bounds in `src/SlipSnap/Services/ToolbarManager.cs`
- [X] T073 [P] Handle virtual desktop API unavailable: `IVirtualDesktopService.IsAvailable` check at startup; disable NextDesktop/PrevDesktop buttons with tooltip "Virtual desktop API unavailable" in `src/SlipSnap/ViewModels/ToolbarViewModel.cs`
- [X] T074 [P] Handle UIAccess degradation: on startup, log whether UIAccess is active; show one-time notification via tray icon if not in `src/SlipSnap/App.xaml.cs`
- [X] T075 [P] Add diagnostic logging calls throughout: startup sequence, UIAccess status, COM init, fullscreen transitions, settings load/save, errors in services that use `ILogger`
- [X] T076 [P] Write E2E screenshot baseline tests for toolbar rendering (default theme + dark theme + custom opacity) in `tests/SlipSnap.E2ETests/ScreenshotTests.cs`
- [X] T077 [P] Create app icon: `src/SlipSnap/Assets/slipsnap.ico` (placeholder icon for tray and window)
- [X] T078 Run `quickstart.md` validation: create solution from scratch per quickstart, build, run unit tests, run E2E tests, verify all pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — **BLOCKS all user stories**
- **US1 (Phase 3)**: Depends on Foundational — delivers MVP
- **US2 (Phase 4)**: Depends on US1 (extends `ToolbarViewModel` and `ToolbarWindow`)
- **US8 (Phase 5)**: Depends on Foundational — can run parallel with US1/US2
- **US3 (Phase 6)**: Depends on US1 (extends `ToolbarWindow`)
- **US4 (Phase 7)**: Depends on US8 (tray icon "Settings" action) and US1 (toolbar to update live)
- **US5 (Phase 8)**: Depends on US4 (settings UI) and US1 (toolbar window)
- **US6 (Phase 9)**: Depends on US5 (toolbar manager multi-toolbar support)
- **US7 (Phase 10)**: Depends on US6 (visibility state machine in ToolbarManager)
- **Polish (Phase 11)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Foundation → implements core toolbar + virtual desktop switching
- **US2 (P1)**: US1 → adds Start Menu + Task View buttons to existing toolbar
- **US8 (P2)**: Foundation → tray icon is independent of toolbar content
- **US3 (P2)**: US1 → adds grip drag to existing toolbar window
- **US4 (P2)**: US1 + US8 → settings dialog modifies existing toolbar + opened from tray icon
- **US5 (P3)**: US4 → multi-edge toolbar config built on settings UI
- **US6 (P3)**: US5 → fullscreen detection controls multi-toolbar visibility
- **US7 (P3)**: US6 → RDP detection feeds into same visibility state machine

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models/interfaces before services
- Services before ViewModels
- ViewModels before Views
- Story complete before moving to next priority

### Parallel Opportunities

**Setup phase**: T004, T005, T006 all parallel
**Foundational phase**: T007–T010, T012–T014, T018–T019 all parallel (different files)
**After Foundation**: US8 (tray icon) can proceed in parallel with US1+US2
**Within each user story**: Tests marked [P] run in parallel, implementation sequential

---

## Parallel Example: User Story 1

```text
# All US1 tests in parallel:
T020: ToolbarViewModel unit tests
T021: KeyboardSimulator unit tests
T022: E2E toolbar visibility test
T023: E2E AppLauncher helper

# Then implementation sequentially:
T024: KeyboardSimulator → T025: VirtualDesktopService → T026: ToolbarViewModel → T027: ToolbarWindow.xaml → T028: ToolbarWindow.xaml.cs → T029: ToolbarManager → T030: App.xaml.cs wiring
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (**CRITICAL** — blocks all stories)
3. Complete Phase 3: US1 — toolbar with desktop switching
4. Complete Phase 4: US2 — add Start Menu + Task View
5. **STOP and VALIDATE**: All 4 buttons work, toolbar stays above fullscreen apps
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Test → Deploy (toolbar + desktop switching = core MVP)
3. US2 → Test → Deploy (+ Start Menu, Task View)
4. US8 → Test → Deploy (+ tray icon with Quit)
5. US3 → Test → Deploy (+ drag repositioning)
6. US4 → Test → Deploy (+ settings dialog)
7. US5 → Test → Deploy (+ multi-toolbar)
8. US6 → Test → Deploy (+ fullscreen-only mode)
9. US7 → Test → Deploy (+ RDP detection)
10. Polish → Final validation
