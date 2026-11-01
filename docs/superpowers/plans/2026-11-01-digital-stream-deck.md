# Digital Stream Deck Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a compact Windows WPF overlay whose six customizable buttons open applications or URLs and whose visibility toggles with `Alt + Mouse Button 4`.

**Architecture:** A dependency-free .NET 10 WPF executable owns the overlay and editing UI. Small services isolate JSON persistence, target validation/launching, and the Win32 low-level mouse hook so risky behavior can be tested without driving the UI.

**Tech Stack:** C# 14, .NET 10, WPF, Win32 P/Invoke, `System.Text.Json`, MSTest

**Spec:** `docs/superpowers/specs/2026-11-01-digital-stream-deck-design.md`

## Global Constraints

- Target Windows 10/11 x64 with `net10.0-windows`; the shipped app project has no external NuGet dependencies.
- The overlay is a dark, borderless, draggable, `Topmost` 3×2 grid and remembers its position.
- `Alt + XBUTTON1` toggles the overlay and consumes both mouse-down and matching mouse-up so the foreground app receives no side effect.
- Buttons support only application and absolute `http/https` URL actions; invalid input never persists or crashes the app.
- Settings live at `%LocalAppData%\DigitalStreamDeck\settings.json`, use temp-file replacement, and a corrupt source file is never silently deleted.
- No macros, key injection, plugin system, multi-page deck, hardware integration, startup registration, installer, polling loop, or exclusive-fullscreen guarantee.

## Review Focus

- A corrupt or unreadable settings file must produce defaults plus a Turkish warning while leaving the original file intact — pinned in Task 2.
- Missing, null, or extra button entries must normalize to exactly six usable buttons — pinned in Task 1.
- A saved position outside all current monitors must be brought back onto the primary working area — pinned in Task 5.
- Repeated `XBUTTON1` down events and the matching up event must be consumed but toggle only once — pinned in Task 4.
- Invalid targets and `Process.Start` failures must return a friendly result rather than throw into the UI — pinned in Task 3.

---

## File Structure

- `DigitalStreamDeck.slnx` — solution entry point.
- `.gitignore` — excludes build output, local brainstorm state, and generated `outputs/` deliverables.
- `src/DigitalStreamDeck/DigitalStreamDeck.csproj` — dependency-free WPF application.
- `src/DigitalStreamDeck/App.xaml`, `App.xaml.cs` — shared theme, startup, top-level exception fallback.
- `src/DigitalStreamDeck/Models/DeckActionType.cs` — supported action enum.
- `src/DigitalStreamDeck/Models/DeckButton.cs` — one tile's editable state.
- `src/DigitalStreamDeck/Models/DeckSettings.cs` — six buttons and optional saved window coordinates.
- `src/DigitalStreamDeck/Services/SettingsService.cs` — defaults, normalization, JSON loading and atomic saving.
- `src/DigitalStreamDeck/Services/ActionLauncher.cs` — validation, `ProcessStartInfo` creation, and safe launch result.
- `src/DigitalStreamDeck/Services/ShortcutGestureFilter.cs` — testable `Alt + XBUTTON1` state/debounce logic.
- `src/DigitalStreamDeck/Services/GlobalShortcutService.cs` — `WH_MOUSE_LL` lifecycle and event dispatch.
- `src/DigitalStreamDeck/MainWindow.xaml`, `MainWindow.xaml.cs` — overlay layout and interactions.
- `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml`, `EditButtonWindow.xaml.cs` — button editor and validation UX.
- `tests/DigitalStreamDeck.Tests/*.cs` — focused model/service tests; no UI automation framework.
- `outputs/DigitalStreamDeck/` — final Release publish output.

### Task 1: Scaffold the solution and settings model

**Files:**
- Create: `.gitignore`
- Create: `DigitalStreamDeck.slnx`
- Create: `src/DigitalStreamDeck/DigitalStreamDeck.csproj`
- Create: `src/DigitalStreamDeck/Models/DeckActionType.cs`
- Create: `src/DigitalStreamDeck/Models/DeckButton.cs`
- Create: `src/DigitalStreamDeck/Models/DeckSettings.cs`
- Create: `tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj`
- Create: `tests/DigitalStreamDeck.Tests/DeckSettingsTests.cs`

**Interfaces:**
- Produces: `enum DeckActionType { None, Application, Url }`.
- Produces: `sealed class DeckButton` with `Id`, `Title`, `Icon`, `Color`, `ActionType`, and `Target` string properties plus `Clone()`.
- Produces: `sealed class DeckSettings` with `List<DeckButton> Buttons`, `double? WindowLeft`, `double? WindowTop`, `CreateDefault()`, and `Normalize()`.

- [ ] **Step 1: Initialize and scaffold**

Run `git init`, create the WPF and MSTest projects with `dotnet new`, create `DigitalStreamDeck.slnx`, add both projects, and reference the app project from tests. Add `.gitignore` entries for `.superpowers/`, `**/bin/`, `**/obj/`, and `outputs/`.

- [ ] **Step 2: Write failing model tests**

Add `CreateDefault_ReturnsSixDistinctButtons`, `Normalize_NullButtons_ReturnsSixUsableButtons`, and `Normalize_TrimsExtraButtonsToSix`; assert six stable unique IDs, non-empty titles/colors, and no null list entries.

- [ ] **Step 3: Verify the tests fail**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "DeckSettingsTests"`

Expected: FAIL because the model types and factories do not exist.

- [ ] **Step 4: Implement the minimal models**

Create the three model files with the interfaces above. `CreateDefault()` supplies six visual examples; `Normalize()` repairs missing entries, preserves valid user entries, trims extras, and fills to exactly six.

- [ ] **Step 5: Verify the task**

Run: `dotnet test DigitalStreamDeck.slnx`

Expected: all model tests PASS and the WPF project builds.

- [ ] **Step 6: Commit**

```powershell
git add .gitignore DigitalStreamDeck.slnx src tests docs/superpowers
git commit -m "feat: scaffold digital stream deck"
```

### Task 2: Persist settings safely

**Files:**
- Create: `src/DigitalStreamDeck/Services/SettingsService.cs`
- Create: `tests/DigitalStreamDeck.Tests/SettingsServiceTests.cs`

**Interfaces:**
- Consumes: `DeckSettings.CreateDefault()` and `DeckSettings.Normalize()` from Task 1.
- Produces: `sealed record SettingsLoadResult(DeckSettings Settings, string? Warning)`.
- Produces: `SettingsService(string? settingsDirectory = null)`, `Task<SettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)`, and `Task SaveAsync(DeckSettings settings, CancellationToken cancellationToken = default)`.

- [ ] **Step 1: Write failing persistence tests**

Use a per-test temporary directory and add `LoadAsync_MissingFile_ReturnsDefaults`, `SaveThenLoad_PreservesButtonsAndPosition`, `LoadAsync_CorruptJson_ReturnsDefaultsWithTurkishWarningAndPreservesFile`, and `LoadAsync_PartialButtons_NormalizesToSix`.

- [ ] **Step 2: Verify the tests fail**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "SettingsServiceTests"`

Expected: FAIL because `SettingsService` is missing.

- [ ] **Step 3: Implement atomic JSON persistence**

Default the directory to `%LocalAppData%\DigitalStreamDeck`. Serialize with `System.Text.Json`; write `settings.json.tmp`, flush/close it, then replace `settings.json`. On load errors return normalized defaults and `Ayarlar yüklenemedi; varsayılan düzen açıldı.` without deleting or overwriting the bad file.

- [ ] **Step 4: Verify the task**

Run: `dotnet test DigitalStreamDeck.slnx`

Expected: all tests PASS, including file preservation after corrupt JSON.

- [ ] **Step 5: Commit**

```powershell
git add src/DigitalStreamDeck/Services/SettingsService.cs tests/DigitalStreamDeck.Tests/SettingsServiceTests.cs
git commit -m "feat: persist deck settings safely"
```

### Task 3: Validate and launch button actions

**Files:**
- Create: `src/DigitalStreamDeck/Services/ActionLauncher.cs`
- Create: `tests/DigitalStreamDeck.Tests/ActionLauncherTests.cs`

**Interfaces:**
- Consumes: `DeckButton` and `DeckActionType` from Task 1.
- Produces: `sealed record LaunchResult(bool Succeeded, string? Error)`.
- Produces: `ActionLauncher(Func<ProcessStartInfo, Process?>? processStarter = null)`, `string? Validate(DeckButton button)`, and `LaunchResult Launch(DeckButton button)`.

- [ ] **Step 1: Write failing action tests**

Add tests for a valid existing `.exe`, missing application path, valid `https` URL, relative/`file`/non-HTTP URL rejection, `None` action rejection, blank or over-24-character title rejection, blank or over-four-text-element icon rejection, correct `UseShellExecute = true`, and an injected starter that throws. Assert exact friendly Turkish errors and that exceptions do not escape.

- [ ] **Step 2: Verify the tests fail**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "ActionLauncherTests"`

Expected: FAIL because `ActionLauncher` is missing.

- [ ] **Step 3: Implement validation and safe launch**

Require a 1–24 character title and a 1–4 Unicode text-element icon. Accept only existing `.exe` files for `Application` and absolute `http/https` URIs for `Url`. Build `ProcessStartInfo` with `UseShellExecute = true`; catch launch failures and return `Hedef açılamadı. Dosya veya adresi kontrol edin.`.

- [ ] **Step 4: Verify the task**

Run: `dotnet test DigitalStreamDeck.slnx`

Expected: all action and prior tests PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/DigitalStreamDeck/Services/ActionLauncher.cs tests/DigitalStreamDeck.Tests/ActionLauncherTests.cs
git commit -m "feat: launch configured targets"
```

### Task 4: Implement the global Alt + Mouse Button 4 gesture

**Files:**
- Create: `src/DigitalStreamDeck/Services/ShortcutGestureFilter.cs`
- Create: `src/DigitalStreamDeck/Services/GlobalShortcutService.cs`
- Create: `tests/DigitalStreamDeck.Tests/ShortcutGestureFilterTests.cs`

**Interfaces:**
- Produces: `enum ShortcutDecision { PassThrough, Consume, ToggleAndConsume }`.
- Produces: `ShortcutGestureFilter.Process(int message, uint mouseData, bool altDown, long timestampMilliseconds)`.
- Produces: `GlobalShortcutService` with `event EventHandler? ToggleRequested`, `Start()`, and `Dispose()`.

- [ ] **Step 1: Write failing gesture tests**

Cover `Alt + XBUTTON1 down => ToggleAndConsume`, matching up => `Consume`, repeated down within 250 ms => `Consume` without another toggle, no Alt => `PassThrough`, and XBUTTON2/ordinary mouse messages => `PassThrough`.

- [ ] **Step 2: Verify the tests fail**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "ShortcutGestureFilterTests"`

Expected: FAIL because the gesture filter is missing.

- [ ] **Step 3: Implement the stateful gesture filter**

Use `WM_XBUTTONDOWN`, `WM_XBUTTONUP`, high-word `XBUTTON1`, a captured-down flag, and a 250 ms debounce. The matching up remains consumed even if Alt is released first.

- [ ] **Step 4: Implement the native hook service**

Use `SetWindowsHookEx(WH_MOUSE_LL)`, `CallNextHookEx`, `UnhookWindowsHookEx`, and `GetAsyncKeyState(VK_MENU)`. Keep the delegate alive, raise `ToggleRequested` only for `ToggleAndConsume`, return nonzero for both consume decisions, fail startup with a friendly `InvalidOperationException`, and make `Dispose()` idempotent.

- [ ] **Step 5: Verify the task**

Run: `dotnet test DigitalStreamDeck.slnx`

Expected: all tests PASS and the native service compiles x64 without warnings caused by pointer truncation.

- [ ] **Step 6: Commit**

```powershell
git add src/DigitalStreamDeck/Services/ShortcutGestureFilter.cs src/DigitalStreamDeck/Services/GlobalShortcutService.cs tests/DigitalStreamDeck.Tests/ShortcutGestureFilterTests.cs
git commit -m "feat: add global overlay shortcut"
```

### Task 5: Build the overlay and editor UI

**Files:**
- Modify: `src/DigitalStreamDeck/App.xaml`
- Modify: `src/DigitalStreamDeck/App.xaml.cs`
- Modify: `src/DigitalStreamDeck/MainWindow.xaml`
- Modify: `src/DigitalStreamDeck/MainWindow.xaml.cs`
- Create: `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml`
- Create: `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml.cs`
- Create: `src/DigitalStreamDeck/Services/WindowPositionNormalizer.cs`
- Create: `tests/DigitalStreamDeck.Tests/WindowPositionNormalizerTests.cs`

**Interfaces:**
- Consumes: all model/service interfaces from Tasks 1–4.
- Produces: `WindowPositionNormalizer.Normalize(double? left, double? top, Rect windowBounds, IReadOnlyList<Rect> workingAreas) -> Point`.
- Produces: a visible-on-first-run `MainWindow` that renders exactly six buttons and owns service lifecycle.
- Produces: `EditButtonWindow(DeckButton draft, ActionLauncher validator)` with `DeckButton? SavedButton`.

- [ ] **Step 1: Write failing position tests**

Add tests for preserved visible coordinates, null coordinates centered in the primary working area, and coordinates outside every current monitor clamped back into the primary working area.

- [ ] **Step 2: Verify the tests fail**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "WindowPositionNormalizerTests"`

Expected: FAIL because the normalizer is missing.

- [ ] **Step 3: Implement position normalization**

Implement the signature above; consider the full overlay rectangle, not only its top-left point, when deciding whether it is visible. In the app, supply current monitor work areas from the framework-provided `System.Windows.Forms.Screen.AllScreens` API.

- [ ] **Step 4: Implement the app lifecycle and theme**

In `App`, load settings before showing the panel, show the load warning after startup, start the hook, and dispose it on exit. Keep the app alive while the window is hidden. Add shared dark colors, button styles, focus indicators, and Turkish copy in `App.xaml`.

- [ ] **Step 5: Implement the compact overlay**

Create a transparent, rounded, borderless, `Topmost` window with a draggable title bar and a 3×2 `ItemsControl`. Left-click launches or edits an unconfigured tile; right-click edits a cloned tile; `Escape` hides; the close button exits. Dispatch hook callbacks onto the WPF dispatcher, persist edits and position, and show friendly save/launch errors.

- [ ] **Step 6: Implement the edit window**

Provide title, icon text, fixed color palette, `Application`/`Url` selection, target field, `.exe` picker, Save, and Cancel. Disable or reject Save using `ActionLauncher.Validate`; never mutate the source tile until Save succeeds.

- [ ] **Step 7: Verify build and tests**

Run: `dotnet test DigitalStreamDeck.slnx; dotnet build DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS and Release build succeeds with zero errors.

- [ ] **Step 8: Perform focused UI smoke checks**

Run the app and verify first-run visibility, dragging, saved position, six tiles, right-click editor, cancel immutability, invalid-input copy, `Escape`, close/exit, and reopening with saved settings. Check at 100% and 150% Windows scaling.

- [ ] **Step 9: Commit**

```powershell
git add src/DigitalStreamDeck tests/DigitalStreamDeck.Tests/WindowPositionNormalizerTests.cs
git commit -m "feat: build stream deck overlay UI"
```

### Task 6: Verify the real gesture and publish the deliverable

**Files:**
- Create: `README.md`
- Create: `outputs/DigitalStreamDeck/` from publish output

**Interfaces:**
- Consumes: the complete application from Tasks 1–5.
- Produces: a runnable Windows x64 Release folder headed by `DigitalStreamDeck.exe`.

- [ ] **Step 1: Run the full automated suite**

Run: `dotnet test DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS.

- [ ] **Step 2: Run the native end-to-end smoke test**

With the app running, verify `Alt + Mouse Button 4` shows/hides exactly once, the foreground browser does not navigate back, releasing Alt before Mouse Button 4 still consumes the up event, a configured URL opens, a selected `.exe` opens, and exclusive-fullscreen limitations are not represented as supported behavior.

- [ ] **Step 3: Document usage and limitations**

Write a concise Turkish `README.md` covering run/build commands, the global shortcut, left/right-click behavior, settings path, clean exit, and the exclusive-fullscreen/elevated-window limitation.

- [ ] **Step 4: Publish**

Run: `dotnet publish src/DigitalStreamDeck/DigitalStreamDeck.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o outputs/DigitalStreamDeck`

Expected: `outputs/DigitalStreamDeck/DigitalStreamDeck.exe` starts successfully on this machine's installed .NET 10 Desktop Runtime.

- [ ] **Step 5: Final verification**

Launch the published executable, repeat the gesture/URL/application/edit/restart smoke path, and confirm no settings are written inside `outputs/`.

- [ ] **Step 6: Commit**

```powershell
git add README.md
git commit -m "docs: add usage and release build"
```
