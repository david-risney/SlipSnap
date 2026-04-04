---
name: uia-testing
description: 'Test and validate Windows desktop WPF apps using UI Automation and screenshots. Use for: verifying toolbar visibility, button functionality, settings dialog, theme switching, opacity, tray icon, drag behavior. Invokes PowerShell scripts that use .NET UIAutomation and System.Drawing for screenshots.'
argument-hint: 'Describe what to test, e.g. "verify toolbar buttons" or "check dark mode settings"'
---

# UIA + Screenshot Testing for Windows Desktop Apps

## When to Use
- Validate WPF app UI behavior after code changes
- Verify toolbar visibility, positioning, button presence
- Check theme switching (light/dark/auto)
- Verify opacity/transparency settings
- Test tray icon context menu
- Capture screenshots for visual verification
- Build end-to-end test scripts

## Available Scripts

| Script | Purpose |
|--------|---------|
| [launch-app.ps1](./scripts/launch-app.ps1) | Build and launch the app, return process info |
| [stop-app.ps1](./scripts/stop-app.ps1) | Stop the app cleanly |
| [find-windows.ps1](./scripts/find-windows.ps1) | Find all SlipSnap windows via UIA |
| [inspect-toolbar.ps1](./scripts/inspect-toolbar.ps1) | Inspect toolbar: buttons, position, size, opacity |
| [take-screenshot.ps1](./scripts/take-screenshot.ps1) | Capture a screenshot of a specific window or full screen |
| [click-button.ps1](./scripts/click-button.ps1) | Click a toolbar button by automation name |
| [open-settings.ps1](./scripts/open-settings.ps1) | Open settings via tray icon right-click |
| [inspect-settings.ps1](./scripts/inspect-settings.ps1) | Read all settings window control states |
| [set-control.ps1](./scripts/set-control.ps1) | Set a settings control value (checkbox, slider, radio) |

## Procedure

### Quick Validation
1. Run `launch-app.ps1` to build and start the app
2. Run `find-windows.ps1` to discover all toolbar windows
3. Run `inspect-toolbar.ps1` to check button visibility, position, opacity
4. Run `take-screenshot.ps1` to capture visual state
5. View the screenshot with `view_image` tool
6. Run `stop-app.ps1` when done

### Settings Validation
1. Launch app, then run `open-settings.ps1`
2. Run `inspect-settings.ps1` to read current values
3. Use `set-control.ps1` to change settings
4. Run `inspect-toolbar.ps1` again to verify live updates
5. Take screenshots before/after for comparison

### Theme Validation
1. Launch app
2. Take screenshot (baseline)
3. Open settings, change theme to Light/Dark
4. Take screenshot (comparison)
5. Inspect toolbar colors and opacity

## Notes
- All scripts are in `./scripts/` relative to this SKILL.md
- Scripts output structured JSON for easy parsing
- Screenshots are saved to `./screenshots/` (gitignored)
- The app must be built before launching (`dotnet build` is included in launch-app.ps1)
- UIA requires the app to be running on the same desktop session
