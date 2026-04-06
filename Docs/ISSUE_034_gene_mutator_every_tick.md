# ISSUE 034: GeneMutator Components Iterate All Defs Every Tick

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Performance |
| **Files** | `1.6/Source/MSSFP/Comps/Map/GeneMutatorMapComponent.cs`, `1.6/Source/MSSFP/Comps/World/GeneMutatorWorldComponent.cs` |

## Description

Both the map and world gene mutator components iterate all `GeneMutatorDef` instances on every tick/update/GUI call. These components call methods on each def that may involve further iteration or computation.

## Impact

Scales linearly with the number of GeneMutatorDefs. If VEE (which provides the defs) adds many mutators, this becomes a performance concern. Running on every tick rather than on a timer multiplies the cost.

## Suggested Fix

1. Add tick interval gating (e.g., only process every 250 ticks).
2. Cache the def list once rather than re-fetching each tick.
3. Separate tick logic from GUI logic -- GUI updates should not drive game state.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Both `GeneMutatorMapComponent` and `GeneMutatorWorldComponent` now gate processing behind a 250-tick interval check. `DefDatabase<GeneMutatorDef>.AllDefsListForReading` cached in a static field; the list is no longer re-fetched on every tick.
