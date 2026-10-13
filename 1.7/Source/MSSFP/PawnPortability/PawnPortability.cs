using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Export;
using MSSFP.PawnPortability.Import;
using MSSFP.PawnPortability.Settings;
using RimWorld;
using Verse;

namespace MSSFP.PawnPortability
{
    /// <summary>
    /// Single public entry point for all pawn portability operations.
    /// Delegates to specialized internal classes.
    /// </summary>
    public static class PawnPortability
    {
        // ── Export ──────────────────────────────────────────────

        public static bool Export(Pawn pawn, string filePath, PawnTemplateMode mode,
            ExportMetadata metadata = null) =>
            PawnExporter.Export(pawn, filePath, mode, metadata);

        public static string ExportToString(Pawn pawn, PawnTemplateMode mode,
            ExportMetadata metadata = null) =>
            PawnExporter.ExportToString(pawn, mode, metadata);

        // ── Validation ─────────────────────────────────────────

        public static ValidationReport Validate(PawnTemplateDef def) =>
            PawnTemplateValidator.Validate(def);

        public static ValidationReport ValidateFile(string filePath)
        {
            // Phase 2: runtime file import
            throw new NotImplementedException(
                "ValidateFile requires PawnTemplateXmlReader (Phase 2)");
        }

        // ── Spawning ───────────────────────────────────────────

        public static Pawn Spawn(PawnTemplateDef def, IntVec3 position, Map map,
            Faction faction = null, PawnKindDef kindOverride = null) =>
            PawnTemplateSpawner.Spawn(def, position, map, faction, kindOverride);

        public static Pawn Spawn(string defName, IntVec3 position, Map map,
            Faction faction = null, PawnKindDef kindOverride = null)
        {
            PawnTemplateDef def = GetDef(defName);
            if (def == null)
            {
                ModLog.Error($"[PawnPortability] Def not found: {defName}");
                return null;
            }
            return Spawn(def, position, map, faction, kindOverride);
        }

        public static Pawn Create(PawnTemplateDef def, Faction faction = null,
            PawnKindDef kindOverride = null) =>
            PawnTemplateSpawner.Create(def, faction, kindOverride);

        public static Pawn Create(string defName, Faction faction = null,
            PawnKindDef kindOverride = null)
        {
            PawnTemplateDef def = GetDef(defName);
            if (def == null)
            {
                ModLog.Error($"[PawnPortability] Def not found: {defName}");
                return null;
            }
            return Create(def, faction, kindOverride);
        }

        // ── Runtime Import (Phase 2) ──────────────────────────

        public static PawnTemplateDef LoadFromFile(string filePath) =>
            Import.PawnTemplateXmlReader.ReadFromFile(filePath);

        public static Pawn SpawnFromFile(string filePath, IntVec3 position, Map map,
            Faction faction = null)
        {
            PawnTemplateDef def = LoadFromFile(filePath);
            if (def == null) return null;
            return Spawn(def, position, map, faction);
        }

        // ── Query ──────────────────────────────────────────────

        public static IEnumerable<PawnTemplateDef> AllDefs =>
            DefDatabase<PawnTemplateDef>.AllDefs;

        /// <summary>
        /// All mod-provided templates (DefDatabase) plus any user-exported templates
        /// loaded from SaveDataFolderPath/ExportedPawns/. Use this for incident candidate
        /// pools so user characters can appear in events.
        /// </summary>
        public static IEnumerable<PawnTemplateDef> AllDefsIncludingUser =>
            AllDefs.Concat(UserPawnTemplateRegistry.UserDefs);

        public static IReadOnlyList<PawnTemplateDef> UserDefs =>
            UserPawnTemplateRegistry.UserDefs;

        public static PawnTemplateDef GetDef(string defName) =>
            DefDatabase<PawnTemplateDef>.GetNamedSilentFail(defName)
            ?? UserPawnTemplateRegistry.UserDefs.FirstOrDefault(d => d.defName == defName);

        public static IEnumerable<PawnTemplateDef> GetDefsByTag(string tag) =>
            AllDefs.Where(d => d.tags?.Contains(tag) == true);

        public static IEnumerable<PawnTemplateDef> GetDefsBySeries(string series) =>
            AllDefs.Where(d => d.originSeries == series);

        // ── Random Generation ─────────────────────────────────

        public static bool IsExcluded(PawnTemplateDef def) =>
            PawnPortabilitySettings.ExcludedTemplateDefNames.Contains(def.defName);

        /// <summary>
        /// True when a template may be picked by a random source (raid injection,
        /// template wanderer join): not excluded in settings, not yet generated this
        /// game, and no live pawn carries its identity (backstop for older saves).
        /// </summary>
        public static bool IsAvailableForRandomGeneration(PawnTemplateDef def) =>
            def != null
            && !IsExcluded(def)
            && TemplateGenerationTracker.Instance?.HasGenerated(def.defName) != true
            && !IsAlive(def);

        public static IEnumerable<PawnTemplateDef> AvailableForRandomGeneration =>
            AllDefsIncludingUser.Where(IsAvailableForRandomGeneration);

        /// <summary>
        /// Records that a random source produced this template's pawn: consumes the
        /// template for the rest of the game and primes the live-pawn cache so a
        /// same-tick check can't pick it again before the pawn spawns.
        /// </summary>
        public static void RegisterRandomGeneration(PawnTemplateDef def, Pawn pawn)
        {
            TemplateGenerationTracker.Instance?.MarkGenerated(def.defName);
            livePawnCache[def] = (Find.TickManager?.TicksGame ?? 0, pawn);
        }

        // ── Duplicate Prevention ──────────────────────────────

        // Cache of the last live-pawn resolution per template def, keyed by def and
        // refreshed after a short TTL. FindLivePawn is invoked from storyteller incident
        // checks (CanFireNowSub) once per template per evaluation, which without caching
        // means a full all-maps + world-pawns scan per template per check.
        private const int LivePawnCacheTtlTicks = 60;
        private static readonly Dictionary<PawnTemplateDef, (int tick, Pawn pawn)> livePawnCache = new();

        public static bool IsAlive(PawnTemplateDef def)
        {
            return FindLivePawn(def) != null;
        }

        public static Pawn FindLivePawn(PawnTemplateDef def)
        {
            if (def?.name == null) return null;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (livePawnCache.TryGetValue(def, out var cached)
                && now - cached.tick < LivePawnCacheTtlTicks)
                return cached.pawn;

            Pawn found = FindLivePawnUncached(def);
            livePawnCache[def] = (now, found);
            return found;
        }

        private static Pawn FindLivePawnUncached(PawnTemplateDef def)
        {
            string first = def.name.first;
            string last = def.name.last;

            bool NameMatches(Pawn p)
            {
                // Raw name: the Name getter returns the colour-decorated copy.
                if (HarmonyPatches.Pawn_Patch.RawName(p) is not NameTriple nt) return false;
                return string.Equals(nt.First, first, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(nt.Last, last, StringComparison.OrdinalIgnoreCase);
            }

            // Search all maps
            foreach (Map map in Find.Maps)
            {
                Pawn found = map.mapPawns.AllPawnsSpawned.FirstOrDefault(NameMatches);
                if (found != null) return found;
            }

            // Search world pawns
            foreach (Pawn worldPawn in Find.WorldPawns.AllPawnsAlive)
            {
                if (NameMatches(worldPawn)) return worldPawn;
            }

            return null;
        }
    }
}
