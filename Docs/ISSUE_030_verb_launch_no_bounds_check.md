# ISSUE 030: Verb_LaunchProjectile Patch Sets Position Without Bounds Check

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/Verb_LaunchProjectile_Patch.cs` |
| **Lines** | ~235 |

## Description

The recoil knockback postfix directly sets `caster.Position` to a new cell without checking:
1. Whether the new position is within map bounds.
2. Whether the new position is passable (not inside a wall or impassable terrain).
3. Whether the caster can physically occupy the destination cell.

## Impact

- Setting position to an out-of-bounds cell causes an `IndexOutOfRangeException` or silent corruption.
- Knockback into a wall could place the pawn inside impassable terrain, breaking pathfinding and rendering.
- The pawn may become stuck or invisible.

## Suggested Fix

Validate the target cell before setting position:

```csharp
IntVec3 newPos = CalculateKnockbackPosition(...);
if (newPos.InBounds(map) && newPos.Standable(map))
{
    caster.Position = newPos;
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`InBounds(caster.Map)` + `Standable(caster.Map)` check added before assigning the knockback position. Out-of-bounds and impassable-cell destinations are now silently ignored rather than applied.
