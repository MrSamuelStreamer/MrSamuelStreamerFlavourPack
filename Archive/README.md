# Archive

Content preserved from earlier mods. Nothing in this folder is loaded by RimWorld: `Archive/` is
deliberately absent from `../loadFolders.xml`, so these files cost nothing at startup and cannot
throw cross-reference errors.

Files sit here until their dependencies are verified to resolve, at which point they are copied into
a live, mod-gated `Defs/` folder.

## GDFP-StructureDefs

Viewer-submitted KCSG structures from GoldenDuckFlavourPack.

| | |
|---|---|
| Source repo | https://github.com/MrSamuelStreamer/GoldenDuckFlavourPack |
| Source path | `1.5/Defs/StructureDefs` |
| Commit | `dae4d8079456c16b3d6419a19f680bc2e845a0d9` (branch `main`) |
| Retrieved | 2026-07-20 |
| Contents | 46 XML plus the original `README.md`: 52 `KCSG.StructureLayoutDef`, 771 `KCSG.SymbolDef`, 4 `GenStepDef` |
| Authors | 19 viewers; subdirs `chaosengine/`, `dissonant_one/`, `vanora/`, `ingedum/`, `soaringturtle/` |

These defs were authored against RimWorld 1.5 and have not been audited wholesale for 1.6 def drift.
Some `ThingDef` and `TerrainDef` references may have been renamed or removed since.

### Modifications from source

One change, applied to all 52 occurrences so that promoting a file is a straight copy:

```
Class="GDFP.StructureDefModExtension"  →  Class="MSSFP.ModExtensions.StructureDefModExtension"
```

Everything else is verbatim.

### Known non-portable references

Four `GenStepDef`s name GDFP-only classes that were not ported into MSSFP:

- `GDFP.GenStep_GDCustomGen` in `genstep.xml`
- `GDFP.GenStep_GDCustomStructureGen` in `GDFP_CuckRoom.xml`, `GDFP_FroggeSanctuary.xml`, `vanora/sgc.xml`

These depended on GDFP's gate and pocket-map system, which MSSFP does not reproduce. MSSFP surfaces
the structures through settlement and quest-site generation instead. Strip or rewrite these
GenStepDefs if any of those three layouts is ever promoted.

### Promotion gating

16 of the 52 layouts carry `<modRequirements>`, naming 39 packageIds between them: all four Ludeon
DLCs, roughly 14 Vanilla Expanded modules, the `sarg.alpha*` mods, and ReGrowth. Two more entries are
self-references. `mss.flavourpack` is MSSFP's own id and resolves; `mssg.flavourpack` was GDFP's id
and is dead in MSSFP, so it is dropped from any layout promoted to the live set.

A layout may only be promoted into
`Compatibility/OskarPotocki.VanillaFactionsExpanded.Core/1.6/Defs/StructureDefs/` when every
`SymbolDef` it references resolves against the mods that are guaranteed present. Layouts needing
third-party mods stay archived until someone adds a matching `IfModActive` folder.
