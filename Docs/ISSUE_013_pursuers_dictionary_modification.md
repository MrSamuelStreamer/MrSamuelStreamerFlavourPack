# ISSUE 013: ScenPart_Pursuers Iterates Dictionary Keys While Potentially Modifying

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Questing/ScenPart_Pursuers.cs` |
| **Lines** | ~370 |

## Description

Code iterates `mapWarningTimers.Keys` while the loop body may modify the dictionary. Modifying a dictionary during enumeration throws `InvalidOperationException` in .NET.

## Impact

Runtime crash (`InvalidOperationException: Collection was modified`) when the modification path is hit during iteration. This may be intermittent depending on game state.

## Suggested Fix

Snapshot the keys before iterating:

```csharp
foreach (var key in mapWarningTimers.Keys.ToList())
{
    // safe to modify mapWarningTimers here
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`.ToList()` snapshot added before iterating `mapWarningTimers.Keys` and `mapRaidTimers.Keys`. The loop body can now freely modify the dictionary without triggering `InvalidOperationException`.
