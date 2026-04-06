# ISSUE 032: HediffSwitchMapComponent Directly Swaps Comps in Hediff List

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | RimWorld Best Practices |
| **File** | `1.6/Source/MSSFP-VFE/HediffSwitchMapComponent.cs` |

## Description

The component directly modifies `hediff.comps` (the internal list of hediff comps) to swap one comp for another. Similar to ISSUE 006, this bypasses the hediff system's internal management, potentially leaving caches and state inconsistent.

## Impact

- Hediff comp initialization/cleanup callbacks may not fire correctly.
- Save/load behavior may be unpredictable if the comp swap happens between save and load.
- Other mods or vanilla code that cached references to the old comp will hold stale references.

## Suggested Fix

Remove the old hediff entirely and add a new one with the desired comp configuration, rather than mutating the comp list in-place.


## Resolution

**Status**: 🔶 PARTIAL — 2026-04-06 (`misc_fixes`)

`DefDatabase<HediffDef>.AllDefsListForReading.FirstOrDefault(...)` scan replaced with a static cached field (issue 018 overlap). The direct `hediff.comps` list swap is retained — removing and re-adding the full hediff would destroy the `asexualFissionCounter` value that the replacement comp reads on initialisation. A guard was added to skip pawns that already have the target comp, preventing duplicate application.
