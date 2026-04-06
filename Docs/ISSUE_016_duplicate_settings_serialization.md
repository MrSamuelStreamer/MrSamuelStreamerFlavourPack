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

**Status**: ⏸ DEFERRED

Blocked by issue 015. Once the `Tabs` static-vs-instance question is resolved and ownership is clear, the duplicate serialisation sites can be consolidated to a single authoritative location per field.
