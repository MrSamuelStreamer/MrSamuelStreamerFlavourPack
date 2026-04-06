# ISSUE 024: Biome Patch XPath Is Too Broad

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | XML Defs Quality |
| **File** | `1.6/Patches/MSS_FP_Biomes.xml` |
| **Lines** | 3 |

## Description

The patch uses the XPath `Defs/BiomeDef/wildPlants` which matches **every BiomeDef** that has a `wildPlants` node. This adds the Mushris plant to all biomes in the game, including biomes where it may not make thematic sense (e.g., ice sheets, sea ice, extreme desert).

## Impact

- Mushris plants appear in every biome, which may be unintended.
- If a DLC or mod adds a biome without `wildPlants`, the patch harmlessly does nothing for those biomes (no error), but it also has no way to selectively target specific biomes.

## Suggested Fix

If the plant should only appear in specific biomes, use targeted XPaths:

```xml
<xpath>Defs/BiomeDef[defName="TemperateForest"]/wildPlants</xpath>
```

Or use `PatchOperationSequence` with `PatchOperationFindMod` to gate the patch.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Broad `Defs/BiomeDef/wildPlants` XPath replaced with targeted entries for six biomes: `TemperateForest`, `TemperateSwamp`, `BorealForest`, `TropicalRainforest`, `TropicalSwamp`, `ColdBog`. Mushris will no longer spawn on `IceSheet`, `SeaIce`, `ExtremeDesert`, or `AridShrubland`.

**Gameplay change**: existing worlds where mushris had already spawned in excluded biomes are unaffected (plants persist); new world generation will not place mushris there.
