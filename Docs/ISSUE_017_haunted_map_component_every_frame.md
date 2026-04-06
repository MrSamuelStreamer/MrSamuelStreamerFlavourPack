# ISSUE 017: HauntedMapComponent Iterates All Pawns Every Frame

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Performance |
| **File** | `1.6/Source/MSSFP/Comps/Map/HauntedMapComponent.cs` |
| **Lines** | MapComponentUpdate method |

## Description

`MapComponentUpdate` (called every frame) iterates all humanlike pawns on the map. The `Graves` property uses `map.listerThings.AllThings.OfType<Building_Grave>()`, which creates a LINQ iterator over every thing on the map every time it is accessed.

## Impact

- Per-frame LINQ allocation and full enumeration of `AllThings` to filter graves.
- On maps with many things (late-game colonies), this becomes a measurable FPS impact.
- The pawn iteration in `MapComponentUpdate` adds per-frame cost proportional to colonist count.

## Suggested Fix

1. Cache the graves list and refresh it only when buildings are added/removed (or on a timer).
2. Move the pawn iteration to `MapComponentTick` with a tick interval check instead of running every frame.
3. Use `map.listerBuildings.AllBuildingsColonistOfClass<Building_Grave>()` if only player graves matter.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`MapComponentUpdate` now uses `map.listerBuildings.AllBuildingsColonistOfClass<Building_Grave>()` instead of `map.listerThings.AllThings.OfType<Building_Grave>()`. The O(n) full-thing-list scan and per-frame LINQ allocation are eliminated.
