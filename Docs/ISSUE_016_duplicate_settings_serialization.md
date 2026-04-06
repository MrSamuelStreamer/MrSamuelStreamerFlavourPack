# ISSUE 016: Duplicate Settings Serialization Between Settings and Tabs

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Mod Architecture |
| **File** | `1.6/Source/MSSFP/Settings.cs` |
| **Lines** | 250-289 |

## Description

`Settings.ExposeData()` serializes ~30 fields directly, then calls `settingsTab.ExposeData()` for each tab. Some fields (e.g., `DrawByMrStreamer`, `ShowHiddenPortraits`) are serialized in both `Settings.ExposeData()` and the corresponding `SettingsTab.ExposeData()`.

When the same field is serialized twice with the same Scribe key, the second call overwrites the first during loading. During saving, both writes produce the same value. This is harmless when everything works, but creates confusion about which serialization is authoritative and risks desync if the field is modified between the two calls.

## Impact

- Maintenance confusion: developers may update one serialization site but not the other.
- If a tab's `ExposeData()` uses a different default value than `Settings.ExposeData()`, loading behavior becomes order-dependent.

## Suggested Fix

Serialize each field in exactly one place. Either keep all serialization in `Settings.ExposeData()` or move each field's serialization exclusively into its owning tab.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Three categories of duplication resolved:

1. **Settings.ExposeData() vs MainSettingsTab**: Removed `WanderDelayIncludeHumanoids`, `EnableColonistPortraitHiding`, `ShowHiddenPortraits`, and `Enable10SecondsToSpeed` from `Settings.ExposeData()`. These are now serialised exclusively in `MainSettingsTab.ExposeData()`.

2. **Tab-vs-tab (`DrawByMrStreamer`)**: Removed from `MemesSettingsTab.ExposeData()`. It is now serialised exclusively in `MainSettingsTab.ExposeData()`, which is the appropriate home for a display/rendering toggle.

3. **Settings-only fields**: Fields with no tab home (`EnableWanderDelayModification`, `WanderDelayTicks`, `Active10SecondsToSpeed`, recoil fields, reformation points fields, etc.) remain in `Settings.ExposeData()` — they are correctly serialised in exactly one place.
