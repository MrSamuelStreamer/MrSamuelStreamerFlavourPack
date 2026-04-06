# ISSUE 036: ScenPart_Pursuers Missing Translation Keys

| Field | Value |
|---|---|
| **Severity** | LOW |
| **Category** | Localisation |
| **File** | `1.6/Source/MSSFP/Questing/ScenPart_Pursuers.cs`, `Common/Languages/English/Keyed/MSS_FP_Misc.xml` |
| **Found during** | In-game testing, 2026-04-06 |

## Description

Two translation keys used in `ScenPart_Pursuers.DoEditInterface` are missing from the English keyed XML:

| Key | Used at | Current result |
|---|---|---|
| `MSSFP_Pursuer_Mutators` | Button label for the mutators list section | Displays raw key string in UI |
| `MSSFP_Pursuer_MutatorsRemove` | Remove button for each mutator entry | Displays raw key string in UI |

All other `MSSFP_Pursuer_*` keys are present in `MSS_FP_Misc.xml`.

## Impact

The mutators section of the Pursuers scenario editor shows untranslated key strings instead of human-readable labels. Functional impact is nil — the buttons still work — but it looks broken.

## Suggested Fix

Add the two missing entries to `Common/Languages/English/Keyed/MSS_FP_Misc.xml` alongside the existing `MSSFP_Pursuer_*` keys:

```xml
<MSSFP_Pursuer_Mutators>Mutators</MSSFP_Pursuer_Mutators>
<MSSFP_Pursuer_MutatorsRemove>Remove {0}</MSSFP_Pursuer_MutatorsRemove>
```

## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Both keys added to `Common/Languages/English/Keyed/MSS_FP_Misc.xml` alongside the existing `MSSFP_Pursuer_*` block.
