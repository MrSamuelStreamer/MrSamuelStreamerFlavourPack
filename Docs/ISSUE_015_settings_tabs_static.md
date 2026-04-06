# ISSUE 015: Settings.Tabs Is Static, Shared Across Instances

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Mod Architecture |
| **File** | `1.6/Source/MSSFP/Settings.cs` |
| **Lines** | 143 |

## Description

```csharp
protected static List<SettingsTab> Tabs = new();
```

`Tabs` is static, meaning all `Settings` instances share the same tab list. Since RimWorld may create multiple `Settings` instances during its lifecycle (e.g., during mod list changes, settings resets), the constructor adds tabs to the shared list each time. The `Tabs.Any(t=>t.GetType() == type)` check at line 130 prevents exact duplicates, but the static nature means:

1. Tabs from a previous `Settings` instance persist when a new one is created.
2. Tabs hold references to the old `Settings` instance (passed via constructor), which may be stale.

## Impact

Potential stale reference issues where tabs reference a `Settings` instance that is no longer the active one. In practice this may not manifest because RimWorld typically creates `Settings` once, but the design is fragile.

## Suggested Fix

Make `Tabs` an instance field, or clear it in the constructor before populating.


## Resolution

**Status**: ⏸ DEFERRED

Changing `Tabs` from `static` to an instance field risks clearing the tab list on all live `Settings` instances created during the same session, which could silently discard tab state. Fix deferred until a safe settings initialisation sequence can be established.
