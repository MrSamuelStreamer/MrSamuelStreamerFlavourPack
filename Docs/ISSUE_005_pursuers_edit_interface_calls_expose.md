# ISSUE 005: ScenPart_Pursuers.DoEditInterface Calls ExposeData

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Questing/ScenPart_Pursuers.cs` |
| **Lines** | ~627 |

## Description

`DoEditInterface()` (a UI rendering method called every frame while the scenario editor is open) calls `ExposeData()`. `ExposeData()` is RimWorld's serialization method meant to be called exclusively by the Scribe system during save/load operations. Calling it outside that context is incorrect and can cause:

1. Silent data corruption if Scribe.mode is not set correctly.
2. Unexpected state mutations during UI rendering.
3. Crashes if Scribe expects to be in a specific serialization context.

## Impact

Potential save corruption or undefined behavior when the scenario editor is open. The UI rendering path should never trigger serialization logic.

## Suggested Fix

Remove the `ExposeData()` call from `DoEditInterface()`. If state synchronization is needed, use a dedicated method that copies values without invoking the Scribe system.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`ExposeData()` call removed from `DoEditInterface()`. The UI rendering path no longer touches the Scribe serialisation system.
