# Expandable Shortcut Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the compact WPF Stream Deck with unlimited six-button pages, `.lnk` shortcut support, automatic Windows application icons, and safe add/delete workflows.

**Architecture:** Keep the existing dependency-free WPF/code-behind structure. Store every button in the existing ordered settings list, calculate six-item pages with a pure helper, retrieve icons through Windows Shell, and apply add/edit/delete snapshots only after `SettingsService` persists them successfully.

**Tech Stack:** C# 14, .NET 10, WPF, Win32 Shell P/Invoke, `System.Text.Json`, MSTest

**Spec:** `docs/superpowers/specs/2026-11-01-expandable-shortcut-pages-design.md`

## Global Constraints

- Preserve the current dark, borderless, draggable, `Topmost` 3×2 panel size and the existing `Alt + XBUTTON1` behavior.
- Support any practical number of buttons as ordered pages of six; no hard six-button cap and no persisted current-page state.
- Application actions accept only existing `.exe` and `.lnk` files; URL actions remain absolute `http/https` only.
- Use Windows Shell `SHGetFileInfo` for application/shortcut icons, release native `HICON` handles, cache only for process lifetime, and fall back to the stored emoji/text.
- Existing settings and all corrupt-file recovery guarantees remain backward compatible; an intentionally empty button list stays empty.
- Add/edit/delete changes become visible only after atomic settings persistence succeeds.
- No external NuGet dependency, shortcut COM resolver, keyboard macro, `.url` support, favicon download, drag reordering, installer, or extra page model.

## Review Focus

- A saved empty list must remain empty after reload/normalization instead of recreating defaults — pinned in Task 1.
- Lists longer than six and duplicate/missing IDs must retain every valid entry with stable unique IDs — pinned in Task 1.
- Page calculations after deleting the only item on the last page must clamp to the nearest valid page — pinned in Task 1.
- Uppercase `.LNK` must be accepted while missing links, directories, `.url`, and arbitrary files are rejected — pinned in Task 2.
- Missing/stale icon paths and Shell extraction failure must return null/fallback without crashing or hiding the emoji — pinned in Task 3 and verified visually in Task 6.

---

## File Structure

- Modify `src/DigitalStreamDeck/Models/DeckButton.cs` — add backward-compatible `IconPath`.
- Modify `src/DigitalStreamDeck/Models/DeckSettings.cs` — remove six-item cap, preserve empty lists, normalize arbitrary entries, and create new drafts.
- Create `src/DigitalStreamDeck/Services/DeckPagination.cs` — pure six-item page math and slicing.
- Modify `src/DigitalStreamDeck/Services/ActionLauncher.cs` — accept and shell-launch `.lnk`.
- Create `src/DigitalStreamDeck/Services/ShellIconService.cs` — Shell icon extraction, handle cleanup, freezing, and process cache.
- Create `src/DigitalStreamDeck/Presentation/DeckTileViewModel.cs` — bind one model plus optional WPF icon image.
- Modify `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml(.cs)` — new/existing modes, `.exe;*.lnk` picker, icon preview, delete result.
- Modify `src/DigitalStreamDeck/MainWindow.xaml(.cs)` — page controls, empty state, add/edit/delete snapshots, icon-backed tile binding.
- Modify `src/DigitalStreamDeck/App.xaml.cs` — share one Shell icon service with the main window.
- Modify `README.md` — document pages, add/delete, `.lnk`, and automatic icons.
- Extend focused files under `tests/DigitalStreamDeck.Tests/` for models, pagination, launcher, icons, and editor policy.

### Task 1: Make settings unbounded and add pure pagination

**Files:**
- Modify: `src/DigitalStreamDeck/Models/DeckButton.cs`
- Modify: `src/DigitalStreamDeck/Models/DeckSettings.cs`
- Create: `src/DigitalStreamDeck/Services/DeckPagination.cs`
- Modify: `tests/DigitalStreamDeck.Tests/DeckSettingsTests.cs`
- Modify: `tests/DigitalStreamDeck.Tests/SettingsServiceTests.cs`
- Create: `tests/DigitalStreamDeck.Tests/DeckPaginationTests.cs`
- Include: `docs/superpowers/specs/2026-11-01-expandable-shortcut-pages-design.md`
- Include: `docs/superpowers/plans/2026-11-01-expandable-shortcut-pages.md`

**Interfaces:**
- Produces: `DeckButton.IconPath: string` with empty default and clone support.
- Produces: `DeckSettings.CreateNewButton()` returning a GUID-ID draft titled `Yeni tuş`, icon `+`, blue color, `None` action, empty target/icon path.
- Produces: `DeckSettings.Normalize()` that preserves all non-null entries in order, preserves an empty list, repairs arbitrary missing fields, and stabilizes unique IDs.
- Produces: `static class DeckPagination` with `const int PageSize = 6`, `GetPageCount(int itemCount)`, `ClampPage(int requestedPage, int itemCount)`, and `GetPage<T>(IReadOnlyList<T> items, int pageIndex)`.

- [ ] **Step 1: Write failing model and pagination tests**

Replace the old six-item trimming/null-default expectations and add: `Normalize_MoreThanSix_PreservesAllInOrder`, `Normalize_EmptyList_RemainsEmpty`, `Normalize_ArbitraryEntries_RepairsStableUniqueIds`, `Clone_PreservesIconPath`, `CreateNewButton_ReturnsUniqueUsableDraft`; `SaveThenLoad_EmptyButtons_RemainsEmpty` and `LoadAsync_LegacyJsonWithoutIconPath_UsesEmptyFallback`; page-count/slice cases for 0, 1, 6, 7, 12, and 13 items; negative/high-page clamping; and deletion from a seven-item list clamping page 1 back to page 0.

- [ ] **Step 2: Verify focused RED**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "DeckSettingsTests|DeckPaginationTests"`

Expected: FAIL because the current model trims to six and pagination/new interfaces do not exist.

- [ ] **Step 3: Implement the model migration**

Add `IconPath`; make `Normalize()` operate only on entries present after removing nulls, using the first six visual defaults only as per-index field fallbacks and generic `Yeni tuş`/`+`/blue fallbacks afterward. Never repopulate an empty list. Add `CreateNewButton()` with a GUID `N` ID.

- [ ] **Step 4: Implement six-item page math**

`GetPageCount` returns at least 1, `ClampPage` returns a zero-based valid page, and `GetPage` returns at most six ordered items from the clamped page without mutating the source.

- [ ] **Step 5: Verify task**

Run: `dotnet test DigitalStreamDeck.slnx -c Release`

Expected: all existing and new tests PASS; legacy default creation still returns the original six buttons.

- [ ] **Step 6: Commit**

```powershell
git add docs/superpowers src/DigitalStreamDeck/Models src/DigitalStreamDeck/Services/DeckPagination.cs tests/DigitalStreamDeck.Tests
git commit -m "feat: add expandable deck pages"
```

### Task 2: Accept Windows `.lnk` application shortcuts

**Files:**
- Modify: `src/DigitalStreamDeck/Services/ActionLauncher.cs`
- Modify: `tests/DigitalStreamDeck.Tests/ActionLauncherTests.cs`

**Interfaces:**
- Consumes: existing `DeckActionType.Application` and shell execution path.
- Produces: application validation accepting an existing file with case-insensitive `.exe` or `.lnk`; exact error `Mevcut bir .exe veya .lnk dosyası seçin.`.

- [ ] **Step 1: Write failing shortcut tests**

Add acceptance for existing lowercase/uppercase `.lnk`, launch capture proving the exact `.lnk` path and `UseShellExecute = true`, and rejection for missing `.lnk`, a directory named `.lnk`, `.url`, `.txt`, and extensionless files. Update existing application-error assertions to the new exact copy.

- [ ] **Step 2: Verify focused RED**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "ActionLauncherTests"`

Expected: `.lnk` acceptance tests FAIL.

- [ ] **Step 3: Implement minimal validation**

Use `File.Exists` plus an ordinal-ignore-case extension check for only `.exe` and `.lnk`; retain `ProcessStartInfo.UseShellExecute = true` and existing exception containment.

- [ ] **Step 4: Verify task and commit**

Run: `dotnet test DigitalStreamDeck.slnx -c Release`

Expected: all tests PASS.

```powershell
git add src/DigitalStreamDeck/Services/ActionLauncher.cs tests/DigitalStreamDeck.Tests/ActionLauncherTests.cs
git commit -m "feat: open Windows shortcut files"
```

### Task 3: Extract and cache Windows Shell icons

**Files:**
- Create: `src/DigitalStreamDeck/Services/ShellIconService.cs`
- Create: `src/DigitalStreamDeck/Presentation/DeckTileViewModel.cs`
- Create: `tests/DigitalStreamDeck.Tests/ShellIconServiceTests.cs`

**Interfaces:**
- Produces: `sealed class ShellIconService` with `ImageSource? GetIcon(string? path)` and `void Clear(string path)`.
- Produces: `sealed record DeckTileViewModel(DeckButton Button, ImageSource? IconImage)` exposing pass-through `Title`, `Icon`, and `Color` for XAML binding.

- [ ] **Step 1: Write failing icon-service tests**

Add `GetIcon_NullMissingOrDirectory_ReturnsNull`, `GetIcon_KnownWindowsExecutable_ReturnsFrozenImage`, and `GetIcon_RepeatedPath_ReturnsCachedInstance`. Resolve the known executable from `%WINDIR%\System32\notepad.exe`; skip only when the OS fixture truly does not exist.

- [ ] **Step 2: Verify focused RED**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "ShellIconServiceTests"`

Expected: FAIL because `ShellIconService` is missing.

- [ ] **Step 3: Implement Shell extraction safely**

Call Unicode `SHGetFileInfo` with `SHGFI_ICON | SHGFI_LARGEICON`, convert the returned `HICON` with `Imaging.CreateBitmapSourceFromHIcon`, freeze the image, and always call `DestroyIcon` in `finally`. Catch expected file/Shell failures and return null. Cache successful and null results by normalized full path using ordinal-ignore-case semantics for the process lifetime.

- [ ] **Step 4: Add the tile presentation model**

Create `DeckTileViewModel` without mutation or business logic; it carries the original model so click/edit handlers can identify it.

- [ ] **Step 5: Verify task and commit**

Run: `dotnet test DigitalStreamDeck.slnx -c Release; dotnet build DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS and build has zero errors/warnings.

```powershell
git add src/DigitalStreamDeck/Services/ShellIconService.cs src/DigitalStreamDeck/Presentation tests/DigitalStreamDeck.Tests/ShellIconServiceTests.cs
git commit -m "feat: load Windows application icons"
```

### Task 4: Extend the editor for add/delete, `.lnk`, and icon preview

**Files:**
- Modify: `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml`
- Modify: `src/DigitalStreamDeck/Windows/EditButtonWindow.xaml.cs`
- Modify: `tests/DigitalStreamDeck.Tests/EditButtonWindowTests.cs`

**Interfaces:**
- Consumes: `ShellIconService`, `.exe/.lnk` validation, and `DeckSettings.CreateNewButton()`.
- Produces: `EditButtonWindow(DeckButton draft, ActionLauncher validator, ShellIconService iconService, bool allowDelete)`.
- Produces: existing `SavedButton` plus `bool DeleteRequested`.
- Produces: internal `ResolveIconPath(DeckActionType actionType, string target) -> string`, returning the existing application target for `.exe/.lnk` and empty for URL/invalid input.

- [ ] **Step 1: Write failing editor-policy tests**

Add tests that `ResolveIconPath` returns exact `.exe`/`.lnk` targets for Application and empty for URL, empty, or unsupported targets; preserve the existing palette tests.

- [ ] **Step 2: Verify focused RED**

Run: `dotnet test tests/DigitalStreamDeck.Tests/DigitalStreamDeck.Tests.csproj --filter "EditButtonWindowTests"`

Expected: FAIL because the icon-path policy/interface does not exist.

- [ ] **Step 3: Implement application selection and preview**

Change the picker filter to `Uygulamalar ve kısayollar (*.exe;*.lnk)|*.exe;*.lnk`. On browse, set target and application mode, derive `IconPath`, and refresh an image preview through `ShellIconService`; show the emoji preview when Shell extraction is null. On Save, derive `IconPath` from the final action/target before cloning the result.

- [ ] **Step 4: Implement existing-only delete outcome**

Show a red `Tuşu sil` button only when `allowDelete` is true. Ask `“<title>” tuşunu silmek istiyor musunuz?` with Yes/No; on Yes set `DeleteRequested = true`, clear `SavedButton`, and return `DialogResult = true`. New-mode Cancel/Delete never mutates a model.

- [ ] **Step 5: Verify task and commit**

Run: `dotnet test DigitalStreamDeck.slnx -c Release; dotnet build DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS and XAML compiles cleanly.

```powershell
git add src/DigitalStreamDeck/Windows tests/DigitalStreamDeck.Tests/EditButtonWindowTests.cs
git commit -m "feat: edit shortcut icons and deletion"
```

### Task 5: Integrate pages and safe mutations into the overlay

**Files:**
- Modify: `src/DigitalStreamDeck/App.xaml.cs`
- Modify: `src/DigitalStreamDeck/MainWindow.xaml`
- Modify: `src/DigitalStreamDeck/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `DeckPagination`, `DeckTileViewModel`, `ShellIconService`, and the editor outcomes from Tasks 1–4.
- Produces: `MainWindow(..., ShellIconService iconService)` with page navigation, empty state, add, edit, and delete flows.
- Produces: `RefreshPage()` that clamps `currentPageIndex`, binds at most six `DeckTileViewModel` values, updates `‹`, `N / M`, `›`, and empty-state visibility.
- Produces: `PersistButtonsAsync(IReadOnlyList<DeckButton> candidateButtons) -> Task<bool>` that saves a cloned snapshot before replacing `settings.Buttons` and refreshing UI.

- [ ] **Step 1: Replace the footer and tile icon template**

Keep the fixed window and 3×2 UniformGrid. Bind each item as `DeckTileViewModel`; show `Image` when `IconImage` is non-null and emoji text otherwise. Add named previous/indicator/next/add controls and an empty-state message without adding a seventh tile.

- [ ] **Step 2: Implement page refresh and navigation**

Start on page zero, clamp after every mutation, disable edge arrows, and display one-based `N / M`. Update click/right-click/keyboard handlers to unwrap `DeckTileViewModel.Button`.

- [ ] **Step 3: Implement add and edit snapshots**

Add opens a non-delete editor with `CreateNewButton()`. A successful Save builds a cloned candidate list with the new button, persists it, then selects the last page. Existing edits replace only the matching ID after persistence succeeds.

- [ ] **Step 4: Implement delete snapshots**

For `DeleteRequested`, build a candidate list without the selected ID, persist it, then clamp/refresh. If persistence throws, retain the exact visible/current settings list and show the existing Turkish save error.

- [ ] **Step 5: Preserve position/exit behavior and share icon service**

Keep position-only saves serialized by `saveGate`; pass one `ShellIconService` from `App` to the window. Clear an edited/deleted path from the icon cache only when needed; do not change global-hook lifecycle.

- [ ] **Step 6: Verify task**

Run: `dotnet test DigitalStreamDeck.slnx -c Release; dotnet build DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS, zero build errors/warnings.

- [ ] **Step 7: Focused UI smoke**

Run the app and verify empty state, add Cancel, seventh button/page 2, previous/next disabled states, edit, confirmed delete, page clamp, icon/emoji fallback, settings-save failure rollback where feasible, and unchanged global shortcut/close behavior.

- [ ] **Step 8: Commit**

```powershell
git add src/DigitalStreamDeck/App.xaml.cs src/DigitalStreamDeck/MainWindow.xaml src/DigitalStreamDeck/MainWindow.xaml.cs
git commit -m "feat: add paged shortcut management"
```

### Task 6: Document, publish, and verify `.lnk` end to end

**Files:**
- Modify: `README.md`
- Regenerate ignored: `outputs/DigitalStreamDeck/`

**Interfaces:**
- Consumes: complete expandable deck.
- Produces: updated Turkish documentation and a verified framework-dependent single-file win-x64 executable.

- [ ] **Step 1: Run full Release verification**

Run: `dotnet test DigitalStreamDeck.slnx -c Release --no-restore; dotnet build DigitalStreamDeck.slnx -c Release --no-restore`

Expected: all tests PASS and build has zero errors/warnings.

- [ ] **Step 2: Create a temporary real `.lnk` fixture**

In ignored `work/`, create a shortcut to `%WINDIR%\System32\notepad.exe` using Windows Script Host solely for smoke verification. Do not add COM/runtime dependency to product code and remove the fixture afterward.

- [ ] **Step 3: Smoke the development build**

Back up user settings, then verify adding buttons 7 and 8, page navigation, automatic `.exe` and `.lnk` icons, launching the `.lnk`, edit persistence after restart, delete Cancel/confirm, page clamping, emoji fallback, and unchanged `Alt + XBUTTON1`. Restore the exact settings backup and close only task-created processes.

- [ ] **Step 4: Update Turkish README**

Document `+`, page arrows, `N / M`, right-click delete, `.exe/.lnk` selection, Shell icon fallback, settings compatibility, and the existing shortcut/limitations.

- [ ] **Step 5: Publish and repeat critical smoke**

Run: `dotnet publish src/DigitalStreamDeck/DigitalStreamDeck.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o outputs/DigitalStreamDeck`

Launch the published executable and repeat add/page/`.lnk` icon+launch/restart/delete and global-shortcut checks. Confirm settings remain under LocalAppData and no icon/settings file is written under `outputs/`.

- [ ] **Step 6: Clean and commit**

Remove only task-created fixtures/helpers, verify restored settings hash, clean git status except README, and commit:

```powershell
git add README.md
git commit -m "docs: explain pages and shortcut icons"
```
