# Architecture Overview: MrSamuelStreamerFlavourPack (MSSFP)

## Purpose

A large, multi-feature "flavour pack" RimWorld mod designed to accompany MrSamuelStreamer's playthroughs. It adds a wide range of loosely coupled gameplay systems -- haunts, gene mutations, mercenary hiring, pursuit scenarios, colonist portrait hiding, recoil mechanics, reformation points, void insanity, and more -- all toggleable via an in-game settings panel.

## Project Structure

The mod is split across **8 C# projects** (assemblies), compiled into separate DLLs:

| Assembly | Purpose |
|---|---|
| **MSSFP** | Core mod. Entry point, settings, all main features. |
| **MSSFP-Generator** | Compatibility patches for the Genetron mod (Resource Generator). |
| **MSSFP-VFE** | Compatibility with Vanilla Factions Expanded Core (asexual reproduction, hediff switching). |
| **MSSFP-VEE** | Compatibility with Vanilla Events Expanded (purple game conditions, gene mutators). |
| **MSSFP-VET** | Compatibility with VFE Tribals (archotech advancement ritual). |
| **MSSFP-BS** | Compatibility with Big & Small / BetterPrerequisites (gene incorporation limit bypass). |
| **MSSFP-CE** | Compatibility with Character Editor (size gene patches). |
| **MSSFP-BrainwashPatch** | Compatibility with Uncle Boris Brainwash Chair. |

## Load Structure

- `loadFolders.xml` defines conditional loading for 10 compatibility folders via `IfModActive`.
- `About.xml` declares Harmony as the sole hard dependency; `loadAfter` lists 14 mods.
- Each compatibility project produces a separate DLL loaded only when the target mod is active.

## Key Subsystems

### Settings System
`Settings.cs` extends `ModSettings`. Uses reflection to discover `SettingsTab` subtypes across all MSSFP assemblies at construction time. Each tab manages its own UI and serialization via `ExposeData()`. The `Tabs` list is **static**, shared across all `Settings` instances.

### Harmony Patching
`MSSFPMod` constructor calls `harmony.PatchAll()` for attribute-based patches, plus a manual patch for `Dialog_NamePawn.NameContext`. `SettlementDefeatUtility` is dynamically patched/unpatched based on settings. There are **~20 Harmony patch classes** including prefixes, postfixes, and transpilers.

### Component Hierarchy
- **GameComponent**: `MSSFPGameManager` (scheduled tasks), `PersistentGeneGameComponent`
- **WorldComponent**: `ColonistHidingWorldComponent`, `GeneMutatorWorldComponent`, `ReformationPointsWorldComponent`
- **MapComponent**: `HauntedMapComponent`, `LoversRetreatMapComponent`, `MercenaryContractMapComponent`, `GeneMutatorMapComponent`, `PossessionMapComponent`

### Feature Modules (partial list)
- **Haunts**: Hediff-based ghost possession with visual rendering and proximity transfer
- **Mercenary Hiring**: Choice letter + map component tracking contracts
- **Pursuers**: Scenario part with escalating threat timers
- **Gene Mutations**: Growth moment gene injection, gene mutator workers
- **Void Insanity**: Hediff with random mental breaks and thoughts
- **Recoil**: Verb_LaunchProjectile transpiler for knockback/damage
- **Portrait Hiding**: ColonistBar patch to hide/reveal portraits
- **Reformation Points**: Ideology integration for bonus precept points
- **Balloon Transport**: PawnFlyer subclass for balloon travel

## XML Content

- **38 Def XML files** across `1.6/Defs/` covering HediffDefs, GeneDefs, IncidentDefs, ScenPartDefs, ThingDefs (items, buildings, plants, races, weapons), RecipeDefs, FactionDefs, TraitDefs, and more.
- **8 Patch XML files** in `1.6/Patches/` modifying vanilla defs (biomes, binge, furnace, age-up, resurrector, research positions, world, interactions).
- **12 Compatibility XML files** across `Compatibility/` folders.

## Build Configuration

All projects target **.NET Framework 4.8** (not .NET Core). Debug builds enable `Harmony.DEBUG = true` and additional UI debug rendering. The `ModLog` utility compiles debug logging to no-ops in Release builds.
