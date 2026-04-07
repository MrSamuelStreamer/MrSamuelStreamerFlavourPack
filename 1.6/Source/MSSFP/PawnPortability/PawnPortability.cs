using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Export;
using MSSFP.PawnPortability.Import;
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

        public static PawnTemplateDef LoadFromFile(string filePath)
        {
            throw new NotImplementedException(
                "LoadFromFile requires PawnTemplateXmlReader (Phase 2)");
        }

        public static Pawn SpawnFromFile(string filePath, IntVec3 position, Map map,
            Faction faction = null)
        {
            throw new NotImplementedException(
                "SpawnFromFile requires PawnTemplateXmlReader (Phase 2)");
        }

        // ── Query ──────────────────────────────────────────────

        public static IEnumerable<PawnTemplateDef> AllDefs =>
            DefDatabase<PawnTemplateDef>.AllDefs;

        public static PawnTemplateDef GetDef(string defName) =>
            DefDatabase<PawnTemplateDef>.GetNamedSilentFail(defName);

        public static IEnumerable<PawnTemplateDef> GetDefsByTag(string tag) =>
            AllDefs.Where(d => d.tags?.Contains(tag) == true);

        public static IEnumerable<PawnTemplateDef> GetDefsBySeries(string series) =>
            AllDefs.Where(d => d.originSeries == series);

        // ── Duplicate Prevention ──────────────────────────────

        public static bool IsAlive(PawnTemplateDef def)
        {
            return FindLivePawn(def) != null;
        }

        public static Pawn FindLivePawn(PawnTemplateDef def)
        {
            if (def?.name == null) return null;

            string first = def.name.first;
            string last = def.name.last;

            bool NameMatches(Pawn p)
            {
                if (p.Name is not NameTriple nt) return false;
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
