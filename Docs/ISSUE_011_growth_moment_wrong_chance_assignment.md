# ISSUE 011: Growth Moment Patch Assigns Wrong Gene Chance Value

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/Dialog_GrowthMomentChoices_Patch.cs` |
| **Lines** | ~129 |

## Description

The `RandomGeneChance` variable is assigned the value of `NeutralGeneChance` instead of its own setting:

```csharp
// Likely incorrect:
float RandomGeneChance = MSSFPMod.settings.NeutralGeneChance;
// Should be:
float RandomGeneChance = MSSFPMod.settings.RandomGeneChance;
```

## Impact

The "random gene" probability uses the neutral gene chance setting instead of the random gene chance setting. Players adjusting these settings independently will not get the expected behavior -- random gene probability will always mirror neutral gene probability.

## Suggested Fix

Change to `MSSFPMod.settings.RandomGeneChance`.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`GeneType.random` case now reads `MSSFPMod.settings.RandomGeneChance` instead of `NeutralGeneChance`. The two settings are now independent as intended.
