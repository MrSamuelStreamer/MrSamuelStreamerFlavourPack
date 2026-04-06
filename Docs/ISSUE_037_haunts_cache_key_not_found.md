# ISSUE 037: HauntsCache.RebuildCacheForPawn KeyNotFoundException

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Hediffs/HauntsCache.cs` |
| **Line** | 90 |
| **Found during** | In-game testing, 2026-04-06 |

## Description

`RebuildCacheForPawn` clears the `Cache` entry for `p` on line 77 via `ClearCacheForPawn(p)`, then immediately tries to access `Cache[p]` inside the foreach loop (line 90) without re-initialising the entry. This always throws `KeyNotFoundException` whenever a pawn has at least one `HediffComp_Haunt` with a non-zero `SkillBoostLevel`.

```csharp
public static void RebuildCacheForPawn(Pawn p)
{
    ClearCacheForPawn(p);  // removes p from Cache

    // ...

    foreach (HediffComp_Haunt hediffCompHaunt in ...)
    {
        if (!Cache[p].ContainsKey(...))  // KeyNotFoundException — p was just removed!
```

The exception message reports the pawn's name as the missing key (e.g. `'Tail'`), which is how `Pawn.ToString()` renders.

There is a second related bug: `HediffComp_Haunt.skillToBoost` defaults to `null` for haunts that have no skill boost configured. The `foreach` body calls `Cache[p].ContainsKey(hediffCompHaunt.skillToBoost)` with a `null` key, which throws `ArgumentNullException` (`Dictionary<SkillDef, int>` does not accept null keys).

## Impact

`CompPostMake()` / `CompPostPostAdd()` throws on every pawn whenever any haunt hediff is added — both when a haunt has no skill boost (`skillToBoost == null`) and when the cache entry for the pawn hasn't been initialised. Skill boosts from haunts never apply.

## Suggested Fix

Two fixes required:

1. Initialise `Cache[p]` before the foreach loop:
```csharp
Cache[p] = new Dictionary<SkillDef, int>();
```

2. Skip null `skillToBoost` entries inside the loop:
```csharp
if (hediffCompHaunt.skillToBoost == null) continue;
```

## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Both fixes applied to `HauntsCache.RebuildCacheForPawn`:
1. Added `Cache[p] = new Dictionary<SkillDef, int>();` before the foreach loop.
2. Added `if (hediffCompHaunt.skillToBoost == null) continue;` inside the loop.
