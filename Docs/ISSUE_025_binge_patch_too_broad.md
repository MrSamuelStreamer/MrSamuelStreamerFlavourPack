# ISSUE 025: Binge Patch Enables Binging on All Chemicals

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | XML Defs Quality |
| **File** | `1.6/Patches/MSS_Binge.xml` |
| **Lines** | 3-7 |

## Description

The patch uses `Defs/ChemicalDef/canBinge` which matches **every ChemicalDef** and replaces `canBinge` with `true`. This enables binging behavior for all chemicals, including any that were intentionally set to `canBinge=false` by vanilla or other mods.

## Impact

- Overrides the intentional design of chemicals that should not trigger binge mental breaks.
- Affects every chemical from every mod, not just MSSFP's own chemicals.
- No setting toggle to disable this behavior.

## Suggested Fix

1. Target specific ChemicalDefs by defName if only certain chemicals should be bingeable.
2. Consider gating this behind a mod setting.
3. If truly all chemicals should be bingeable, document this as an intentional gameplay change.


## Resolution

**Status**: 📝 DOCUMENTED — 2026-04-06 (`misc_fixes`)

Left as-is per maintainer decision. A comment was added to `MSS_Binge.xml` clarifying that enabling binge for all `ChemicalDef`s is intentional gameplay design for this mod. No functional change made.
