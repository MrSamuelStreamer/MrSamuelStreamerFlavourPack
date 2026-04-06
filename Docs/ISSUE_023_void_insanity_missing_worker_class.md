# ISSUE 023: MentalBreakDef References Non-Existent Worker Class

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | XML Defs Quality |
| **File** | `1.6/Defs/Hediffs/MSS_VoidInsanity.xml` |
| **Lines** | 8-9 |

## Description

The `MSSFP_HumanityBreak` MentalBreakDef references:
- `<workerClass>MentalBreakWorker_HumanityBreak</workerClass>` -- no namespace prefix, and no C# class by this name exists in any MSSFP assembly.
- `<mentalState>HumanityBreak</mentalState>` -- no MentalStateDef by this defName exists in the mod's XML.

The `workerClass` without a namespace would resolve to `RimWorld.MentalBreakWorker_HumanityBreak` or `Verse.MentalBreakWorker_HumanityBreak`, neither of which exists in vanilla RimWorld.

## Impact

- RimWorld will log an error at startup about the unresolvable type.
- The mental break def will be non-functional -- it cannot trigger because neither its worker class nor its mental state def exist.
- The `MSSFP_HumanityBreak` defName is referenced in the VoidInsanity hediff's `MentalStates` list (line 32), so void insanity attempts to trigger a broken mental break 75% weighted.

## Suggested Fix

Either implement the missing `MentalBreakWorker_HumanityBreak` class and `HumanityBreak` MentalStateDef, or remove the broken def and its reference from the VoidInsanity hediff comp.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`MSSFP_HumanityBreak` MentalBreakDef removed from `MSS_VoidInsanity.xml`. Its reference (75% weight) in the VoidInsanity hediff `MentalStates` list also removed. VoidInsanity now triggers only the remaining valid mental breaks in the list. No more startup log error about an unresolvable `workerClass`.
