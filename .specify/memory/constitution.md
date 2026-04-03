# SlipSnap Constitution

## Core Principles

### I. Automated Testing Standards (NON-NEGOTIABLE)
- Every public API surface must have unit tests before merging.
- Win32 interop and COM calls must be abstracted behind interfaces so business logic can be tested without a live desktop session.
- Integration tests exercise real window creation, Z-order, and virtual desktop pinning; these run in a dedicated CI stage or manually on a real Windows desktop.
- Red-Green-Refactor: write a failing test, make it pass, then clean up. No skipping steps.
- Test names describe the scenario and expected outcome, e.g. `PinWindow_WhenCalledOnLoadedWindow_PinsToAllDesktops`.

### II. Code Quality
- C# / .NET 8+ with nullable reference types enabled and warnings-as-errors.
- StyleCop or `.editorconfig` enforced formatting — no style debates in reviews.
- P/Invoke declarations live in a single `NativeMethods` static class (or per-API grouping) with `LibraryImport` where possible.
- No magic numbers — Win32 constants (`WS_EX_NOACTIVATE`, `MA_NOACTIVATE`, `HWND_TOPMOST`, etc.) are named constants or enums.
- Keep files small and focused: one class per file, XAML code-behind limited to UI wiring and event forwarding.
- Prefer composition over inheritance; inject dependencies rather than using static singletons.

### III. User Experience Consistency
- The overlay must never steal focus from the target application (fullscreen RDP, game, etc.) unless the user explicitly intends it (e.g., host desktop-switch buttons).
- Visual feedback (hover, press states) must be immediate (< 16 ms frame) — no perceptible lag on button interaction.
- All interactive elements must be operable by touch, mouse, and keyboard.
- Semi-transparent overlay style must remain consistent across all toolbar instances and monitor configurations.
- Toolbar placement, size, and visibility preferences persist across sessions.

### IV. Performance Requirements
- Cold startup to visible overlay: < 2 seconds on target hardware.
- Memory footprint: < 50 MB working set at steady state.
- No background polling loops — use event-driven approaches (window hooks, COM events, OS notifications) for state changes.
- UI thread must never block on I/O or long-running operations; offload to background tasks.
- Resource cleanup: all HWNDs, COM objects, and event hooks must be deterministically released on shutdown.

### V. Simplicity & Minimalism
- YAGNI: do not add features, abstractions, or extension points until they are needed by a concrete scenario.
- Prefer the Win32/WPF built-in mechanism over third-party libraries when the built-in is sufficient.
- Self-contained deployment: single-directory xcopy deploy to Program Files. No installer framework unless distribution requirements demand it.
- README and inline comments explain "why", not "what" — the code should be clear enough for "what".

## Platform & Security Constraints

- **Target OS**: Windows 10 22H2+ (build 22621) and Windows 11.
- **UIAccess=true** is mandatory for the overlay to sit above fullscreen topmost windows. This requires:
  1. Authenticode-signed exe (cert trusted in `LocalMachine\Root`).
  2. Exe installed in a trusted location (`C:\Program Files\`).
- MSIX packaging is **incompatible** with UIAccess (CreateProcess limitation). Use direct deployment or an MSI/MSIX-free installer.
- Self-signed certs for development only — production release requires a code-signing certificate from a public CA or Microsoft-trusted issuer.
- Undocumented COM interfaces (virtual desktop API) may break between Windows builds. Isolate behind an abstraction and version-test at startup.

## Development Workflow

- Feature branches off `main`; squash-merge via PR.
- PRs require: passing CI build, all new/changed code covered by tests, no new warnings.
- Spec-driven development: spec → plan → tasks → implement. Constitution governs all phases.
- Breaking changes to user-visible behavior require spec amendment and version bump.

## Governance

- This constitution supersedes ad-hoc decisions. Deviations require an explicit amendment with rationale.
- Amendments are versioned, documented in this file, and reviewed via PR.
- All code reviews must verify compliance with these principles.

**Version**: 1.0.0 | **Ratified**: 2026-04-02 | **Last Amended**: 2026-04-02
