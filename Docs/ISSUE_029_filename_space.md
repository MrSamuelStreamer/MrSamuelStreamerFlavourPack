# ISSUE 029: Filename Contains Extraneous Space

| Field | Value |
|---|---|
| **Severity** | LOW |
| **Category** | Mod Architecture |
| **File** | `1.6/Source/MSSFP-BS/HarmonyPatches/CompProperties_IncorporateEffect _Patch.cs` |

## Description

The filename contains a space before `_Patch.cs`: `CompProperties_IncorporateEffect _Patch.cs`. This is likely a typo.

## Impact

- No functional impact (the compiler handles filenames with spaces).
- Confusing when browsing the file system or referencing the file in documentation.
- Some tools may have issues with spaces in filenames.

## Suggested Fix

Rename to `CompProperties_IncorporateEffect_Patch.cs` (remove the space).


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

File renamed from `CompProperties_IncorporateEffect _Patch.cs` to `CompProperties_IncorporateEffect_Patch.cs` via `git mv`. Git history preserved. Any `.csproj` references updated accordingly.
