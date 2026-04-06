# ISSUE 009: Pawn_Patch NameCache Dictionary Never Cleaned

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | Performance, C# Code Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/Pawn_Patch.cs` |
| **Lines** | NameCache dictionary declaration |

## Description

`Pawn_Patch.NameCache` is a `Dictionary<Pawn, Name>` that caches pawn names. Entries are added when `Pawn.Name` is accessed but **never removed** -- not when pawns die, are unloaded, or when maps are abandoned.

Since the dictionary holds strong references to `Pawn` objects (which are large, holding references to health, needs, jobs, equipment, etc.), every pawn that has ever had its name accessed remains in memory for the entire game session.

## Impact

- **Memory leak** that grows linearly with the number of unique pawns encountered.
- In a long-running game with many raids, caravans, and world interactions, this can accumulate thousands of entries.
- The `Pawn.Name` getter is called extremely frequently (every frame for visible pawns in the colonist bar, every social interaction check, etc.), so the dictionary grows fast.

## Suggested Fix

1. Use `ConditionalWeakTable<Pawn, Name>` instead of `Dictionary` so entries are automatically collected when the Pawn is GC'd.
2. Alternatively, hook into pawn destruction/discard events to clean up entries.
3. Consider whether the cache is needed at all -- `Pawn.Name` access is already fast.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`Dictionary<Pawn, Name>` replaced with `ConditionalWeakTable<Pawn, Name>`. `Name` is a reference type in RimWorld, so no wrapper was needed. Entries are now automatically collected when the associated `Pawn` is garbage-collected, eliminating the unbounded growth.
