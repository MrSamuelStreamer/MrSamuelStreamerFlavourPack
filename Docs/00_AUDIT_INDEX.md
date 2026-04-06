# MSSFP Codebase Audit Index

**Date**: 2026-04-05
**Auditor**: Automated (Claude)
**Scope**: Full codebase -- 8 C# assemblies, 38 Def XML files, 8 Patch XML files, 12 Compatibility XML files
**Mode**: Read-only static analysis (no compilation or runtime testing)

**Last Updated**: 2026-04-06 — fixes applied on branch `misc_fixes`

## Summary

| Severity | Count | Fixed | Partial | Deferred/Documented |
|---|---|---|---|---|
| CRITICAL | 3 | 3 | 0 | 0 |
| HIGH | 14 | 12 | 0 | 3 |
| MEDIUM | 17 | 11 | 3 | 3 |
| LOW | 6 | 5 | 0 | 1 |
| **Total** | **40** | **31** | **3** | **7** |

## Files

- [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md) -- High-level mod structure, subsystems, and build configuration

## Issue Index

| # | Severity | Category | Short Title | File(s) | Status |
|---|---|---|---|---|---|
| [001](ISSUE_001_game_manager_unregister_noop.md) | CRITICAL | C# Code Quality | UnregisterAll methods are no-ops | `MSSFPGameManager.cs` | ✅ FIXED |
| [002](ISSUE_002_body_hop_haunt_scribe_keys.md) | CRITICAL | C# Code Quality | Scribe keys have trailing commas | `HediffComp_BodyHopHaunt.cs` | ✅ FIXED |
| [003](ISSUE_003_lovers_retreat_inverted_tick.md) | CRITICAL | C# Code Quality | Inverted tick check (600x perf penalty) | `LoversRetreatMapComponent.cs` | ✅ FIXED |
| [004](ISSUE_004_pursuers_lazy_property_recreated.md) | HIGH | C# Code Quality | Lazy<T> recreated on every property access | `ScenPart_Pursuers.cs` | ✅ FIXED |
| [005](ISSUE_005_pursuers_edit_interface_calls_expose.md) | HIGH | C# Code Quality | DoEditInterface calls ExposeData | `ScenPart_Pursuers.cs` | ✅ FIXED |
| [006](ISSUE_006_haunt_direct_hediff_list_manipulation.md) | HIGH | C# / Best Practices | Direct hediff list manipulation | `HediffComp_Haunt.cs` | ✅ FIXED |
| [007](ISSUE_007_genetron_fragile_iterator_transpilers.md) | HIGH | Harmony Patches | 9 fragile iterator transpilers | `Building_Genetron_Patch.cs` | ⏸ DEFERRED |
| [008](ISSUE_008_transferable_ui_full_method_replace.md) | HIGH | Harmony Patches | Full method replacement via Prefix | `TransferableUIUtility_...Patch.cs` | ⏸ DEFERRED |
| [009](ISSUE_009_pawn_name_cache_memory_leak.md) | HIGH | Performance / C# | NameCache dictionary never cleaned | `Pawn_Patch.cs` | ✅ FIXED |
| [010](ISSUE_010_dialog_growth_moment_lookup_leak.md) | HIGH | Performance / C# | DialogLookup dictionary leaks entries | `Dialog_GrowthMomentChoices_Patch.cs` | ✅ FIXED |
| [011](ISSUE_011_growth_moment_wrong_chance_assignment.md) | HIGH | C# Code Quality | RandomGeneChance uses wrong setting value | `Dialog_GrowthMomentChoices_Patch.cs` | ✅ FIXED |
| [012](ISSUE_012_vfe_copy_pasted_positional_args.md) | HIGH | C# / Architecture | Copy-pasted VEF code with 30 positional args | `HediffComp_TweakedAsexualReproduction.cs` | ⏸ DEFERRED |
| [013](ISSUE_013_pursuers_dictionary_modification.md) | HIGH | C# Code Quality | Dictionary modified during enumeration | `ScenPart_Pursuers.cs` | ✅ FIXED |
| [014](ISSUE_014_mouseover_readout_harmony_debug.md) | MEDIUM | Harmony Patches | [HarmonyDebug] left in release code | `MouseoverReadout_Patch.cs` | ✅ FIXED |
| [015](ISSUE_015_settings_tabs_static.md) | MEDIUM | Mod Architecture | Tabs list is static, shared across instances | `Settings.cs` | ⏸ DEFERRED |
| [016](ISSUE_016_duplicate_settings_serialization.md) | MEDIUM | Mod Architecture | Duplicate field serialization | `Settings.cs` | ⏸ DEFERRED |
| [017](ISSUE_017_haunted_map_component_every_frame.md) | MEDIUM | Performance | Iterates all pawns + LINQ graves every frame | `HauntedMapComponent.cs` | ✅ FIXED |
| [018](ISSUE_018_uncached_def_database_lookups.md) | MEDIUM | Performance | Uncached DefDatabase lookups in tick paths | Multiple files | 🔶 PARTIAL |
| [019](ISSUE_019_empty_catch_blocks.md) | MEDIUM | C# Code Quality | Empty catch blocks swallow exceptions | `DebugLog.cs`, `Building_Genetron_Patch.cs` | ✅ FIXED |
| [020](ISSUE_020_hardcoded_backstory_defnames.md) | MEDIUM | Best Practices | Hardcoded backstory defNames | `ChoiceLetter_HireMercenaries.cs` | ✅ FIXED |
| [021](ISSUE_021_colonist_portrait_reflection_per_call.md) | MEDIUM | Performance | Reflection field access on every UI call | `ColonistPortraitHiding_Patches.cs` | ✅ FIXED |
| [022](ISSUE_022_linq_in_tick_paths.md) | MEDIUM | Performance | LINQ allocations in tick/update paths | Multiple files | 🔶 PARTIAL |
| [023](ISSUE_023_void_insanity_missing_worker_class.md) | MEDIUM | XML Defs Quality | MentalBreakDef references non-existent class | `MSS_VoidInsanity.xml` | ✅ FIXED |
| [024](ISSUE_024_biome_patch_too_broad.md) | MEDIUM | XML Defs Quality | Biome patch XPath too broad | `MSS_FP_Biomes.xml` | ✅ FIXED |
| [025](ISSUE_025_binge_patch_too_broad.md) | MEDIUM | XML Defs Quality | Binge patch enables all chemicals | `MSS_Binge.xml` | 📝 DOCUMENTED |
| [026](ISSUE_026_pawn_flyer_reflection_private_fields.md) | MEDIUM | C# / Best Practices | 6 private fields accessed via reflection | `PawnFlyerBalloon.cs` | ✅ FIXED |
| [027](ISSUE_027_dual_harmony_instances.md) | LOW | Mod Architecture | Harmony instance created twice | `MSSFPMod.cs` | ✅ FIXED |
| [028](ISSUE_028_dead_code_ritual_outcome.md) | LOW | C# Code Quality | Unused IncidentParms variable | `RitualOutcomeEffectWorker_...cs` | ✅ FIXED |
| [029](ISSUE_029_filename_space.md) | LOW | Mod Architecture | Filename contains extraneous space | `CompProperties_...Effect _Patch.cs` | ✅ FIXED |
| [030](ISSUE_030_verb_launch_no_bounds_check.md) | MEDIUM | C# Code Quality | Position set without bounds check | `Verb_LaunchProjectile_Patch.cs` | ✅ FIXED |
| [031](ISSUE_031_reformation_points_null_guards.md) | MEDIUM | C# Code Quality | Missing null guards for Ideology chain | `ReformationPointsWorldComponent.cs` | ✅ FIXED |
| [032](ISSUE_032_hediff_switch_direct_comp_swap.md) | MEDIUM | Best Practices | Direct comp list swap in hediff | `HediffSwitchMapComponent.cs` | 🔶 PARTIAL |
| [033](ISSUE_033_about_xml_missing_load_after.md) | LOW | Architecture / Best Practices | Missing loadAfter for 5 conditional deps | `About.xml` | ✅ FIXED |
| [034](ISSUE_034_gene_mutator_every_tick.md) | MEDIUM | Performance | Gene mutator components iterate defs every tick | `GeneMutator*Component.cs` | ✅ FIXED |
| [035](ISSUE_035_pursuers_faction_null_check.md) | MEDIUM | C# Code Quality | Faction null check missing | `ScenPart_Pursuers.cs` | ✅ FIXED |
| [036](ISSUE_036_pursuers_missing_translations.md) | LOW | Localisation | Missing translation keys in Pursuers editor | `ScenPart_Pursuers.cs`, `MSS_FP_Misc.xml` | ✅ FIXED |
| [037](ISSUE_037_haunts_cache_key_not_found.md) | HIGH | C# Code Quality | HauntsCache RebuildCacheForPawn KeyNotFoundException | `HauntsCache.cs` | ✅ FIXED |
| [038](ISSUE_038_haunt_ghost_not_rendering.md) | HIGH | C# Code Quality | Haunt ghost never renders (field vs property) | `HediffComp_Haunt.cs` | ✅ FIXED |
| [039](ISSUE_039_haunt_alwayson_struct_null.md) | HIGH | C# Code Quality | AlwaysOn always false — IntRange struct compared to null | `HediffCompProperties_Haunt.cs` | ✅ FIXED |
| [040](ISSUE_040_bodyhophaunt_scribe_values_pawninfo.md) | HIGH | C# Code Quality | Scribe_Values used for IExposable PawnInfo — lost on save/load | `HediffComp_BodyHopHaunt.cs` | ⏸ DEFERRED |

## Category Breakdown

| Category | Issues |
|---|---|
| C# Code Quality | 001, 002, 003, 004, 005, 011, 013, 019, 028, 030, 031, 035 |
| Harmony Patches Quality | 007, 008, 014 |
| XML Defs Quality | 023, 024, 025 |
| Mod Architecture | 015, 016, 027, 029, 033 |
| Performance | 009, 010, 017, 018, 021, 022, 034 |
| RimWorld Best Practices | 006, 012, 020, 026, 032 |

## Deferred / Partial Issues

These issues were not fully resolved and should be addressed in a follow-up:

| # | Issue | Reason deferred |
|---|---|---|
| 007 | Genetron iterator transpilers | Rewriting 9 iterator transpilers is unsafe without live IL validation. Catch blocks now log warnings (via 019). |
| 008 | TransferableUIUtility full replace | Transpiler rewrite requires in-game UI validation to confirm parity. |
| 012 | VFE positional constructor args | Adding named args requires confirming parameter names from VEF source to avoid regressions. |
| 015 | Settings.Tabs static field | Fix risks clearing tabs on all live `Settings` instances; needs a settings migration path. |
| 016 | Duplicate settings serialization | Blocked by 015. |
| 022 (partial) | `MSSFPGameManager.GameComponentUpdate` LINQ | Cross-branch conflict on a file already modified in the same PR; easy single-file follow-up. |
| 032 (partial) | HediffSwitchMapComponent comp swap | Full hediff removal would lose `asexualFissionCounter` state needed by the replacement comp. |

## Positive Observations

Not everything is a problem. Several areas of the codebase demonstrate good practices:

1. **ResearchProjectDef_UnlockedDefs_NullSafePatch**: Well-implemented transpiler with proper fallback warning when the target IL pattern is not found. This is the gold standard for how all transpilers in this mod should work.

2. **Conditional loading architecture**: The `loadFolders.xml` / compatibility folder pattern is well-structured. Each compatibility mod gets its own assembly and folder, loaded only when the target mod is active. This avoids hard dependencies.

3. **Settings toggleability**: Most features can be enabled/disabled via the settings UI, which is good for a mod that bundles many unrelated features.

4. **MercenaryContractMapComponent**: Clean implementation with proper null checks and save/load handling.

5. **ColonistHidingWorldComponent**: Simple, focused, correctly implemented.

6. **ModLog utility**: Debug logging that compiles to no-ops in Release builds is a good pattern.

7. **MayRequire/MayRequireAnyOf usage**: The recent commit adding these attributes shows awareness of RimWorld's conditional def loading system.

## Recommended Priority (Remaining)

1. **Address deferred Harmony patches** (007, 008) -- these will break on the next RimWorld or target mod update.
2. **Fix Settings static field + serialization** (015, 016) -- design is fragile, plan migration carefully.
3. **Clear remaining LINQ** (022 partial) -- one-line fix in `MSSFPGameManager.GameComponentUpdate`.
4. **Named args for VFE copy-paste** (012) -- low risk, improves forward compatibility.
