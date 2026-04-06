# ISSUE 033: About.xml Missing loadAfter Entries for Conditional Dependencies

| Field | Value |
|---|---|
| **Severity** | LOW |
| **Category** | Mod Architecture, RimWorld Best Practices |
| **File** | `About/About.xml` |
| **Lines** | 20-36 |

## Description

`loadFolders.xml` references several mods via `IfModActive` that are not listed in `About.xml`'s `<loadAfter>`:

| IfModActive in loadFolders.xml | In loadAfter? |
|---|---|
| `vanillaexpanded.vee` | No |
| `sarg.alphagenes` | No |
| `RedMattis.BetterPrerequisites` | Yes (as `redmattis.bigsmall.core`) |
| `OskarPotocki.VanillaFactionsExpanded.Core` | Yes |
| `oskarpotocki.vfe.tribals` | No |
| `Mlie.ResourceGenerator` | Yes |
| `rebuild.cotr.doorsandcorners` | Yes |
| `vanillaquestsexpanded.generator` | No |
| `void.charactereditor` | Yes |
| `cp.uncle.boris.brainwash.chair` | No |

## Impact

Without `loadAfter`, RimWorld may load MSSFP's compatibility assemblies *before* the target mod, causing `TypeLoadException` or `MissingMethodException` at startup for those compatibility features.

## Suggested Fix

Add all `IfModActive` package IDs to the `<loadAfter>` list in `About.xml`.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Five missing `<loadAfter>` entries added to `About/About.xml`: `vanillaexpanded.vee`, `sarg.alphagenes`, `oskarpotocki.vfe.tribals`, `vanillaquestsexpanded.generator`, `cp.uncle.boris.brainwash.chair`.
