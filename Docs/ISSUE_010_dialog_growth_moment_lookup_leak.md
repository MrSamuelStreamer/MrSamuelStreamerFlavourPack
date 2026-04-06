# ISSUE 010: Dialog_GrowthMomentChoices DialogLookup Leaks Entries

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | Performance, C# Code Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/Dialog_GrowthMomentChoices_Patch.cs` |
| **Lines** | ~36 |

## Description

`DialogLookup` is a static `Dictionary` that maps dialog instances to associated data. Entries are added when a growth moment dialog opens but only removed when `MakeChoices` is called. If a dialog is closed by any other means (player dismissal, game reload, pawn death), the entry remains in the dictionary indefinitely.

## Impact

- Memory leak for each dialog that is not completed through the normal `MakeChoices` path.
- Holds references to dialog objects, which in turn hold references to pawns and growth data.

## Suggested Fix

Add cleanup in the dialog's `Close` or `PreClose` method via an additional Harmony Postfix, or use a `ConditionalWeakTable` keyed on the dialog instance.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Added `Dialog_GrowthMomentChoices_Close_Patch`, a Harmony Postfix on `Dialog_GrowthMomentChoices.Close()`. `DialogLookup` entries are now removed whenever the dialog closes, regardless of whether `MakeChoices` was called.
