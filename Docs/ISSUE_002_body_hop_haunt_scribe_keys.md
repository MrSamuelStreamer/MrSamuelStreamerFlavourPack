# ISSUE 002: HediffComp_BodyHopHaunt Scribe Keys Have Trailing Commas

| Field | Value |
|---|---|
| **Severity** | CRITICAL |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Hediffs/HediffComp_BodyHopHaunt.cs` |
| **Lines** | ~85-86 |

## Description

The `Scribe_Values.Look` calls for `id` and `texPath` use key strings with trailing commas:

```csharp
Scribe_Values.Look(ref id, "id,");
Scribe_Values.Look(ref texPath, "texPath,");
```

The comma is part of the serialization key. This means:
1. The save file will contain XML keys like `<id,>` and `<texPath,>`.
2. While this technically works (XML allows commas in element names), it is unconventional and fragile.
3. If the keys are ever "fixed" to remove the comma, all existing saves will fail to load those values, silently resetting them to defaults.

## Impact

Data corruption risk on any key change. Existing saves are locked into the comma-suffixed key format. This is almost certainly a typo that has become load-bearing.

## Suggested Fix

If there are no existing saves to worry about, remove the commas. If backward compatibility matters, add migration logic that reads both key variants.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Trailing commas removed: `"id,"` → `"id"`, `"texPath,"` → `"texPath"`. No migration logic added — the BodyHopHaunt feature was not yet shipped to players, so no existing saves use the comma-suffixed keys.
