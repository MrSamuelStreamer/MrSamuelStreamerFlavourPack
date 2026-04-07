using System.Linq;
using MSSFP.PawnPortability;
using MSSFP.PawnPortability.Defs;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Incidents
{
    /// <summary>
    /// Rare comedic incident: Codex teleports in, punches a random colonist, teleports out.
    /// Codex ascended to punch a god, missed, and now practises on mortals.
    /// </summary>
    public class IncidentWorker_CodexPunch : IncidentWorker
    {
        private const string CodexDefName = "MSSFP_Pawn_Codex";
        private const int MinSpawnDistance = 15;
        private const int MaxSpawnDistance = 25;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!MSSFPMod.settings.EnableCodexPunch)
                return false;

            if (parms.target is not Map map)
                return false;

            // Need at least one free colonist on the map
            if (!map.mapPawns.FreeColonistsSpawned.Any())
                return false;

            // Codex template must exist
            PawnTemplateDef codexDef = PawnPortability.PawnPortability.GetDef(CodexDefName);
            if (codexDef == null)
                return false;

            // Guard: if a player captured Codex and he's alive somewhere, don't fire
            if (PawnPortability.PawnPortability.IsAlive(codexDef))
                return false;

            return base.CanFireNowSub(parms);
        }

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            return base.ChanceFactorNow(target) * MSSFPMod.settings.CodexPunchChanceMultiplier;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.target is not Map map)
                return false;

            // Pick a random free colonist as the target
            Pawn target = map.mapPawns.FreeColonistsSpawned.RandomElement();
            if (target == null)
                return false;

            // Create Codex from his template — use Ancients faction (neutral, non-player)
            Faction ancients = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients);
            Pawn codex = PawnPortability.PawnPortability.Create(CodexDefName, ancients);
            if (codex == null)
            {
                ModLog.Error("[CodexPunch] Failed to create Codex from PawnTemplateDef");
                return false;
            }

            // Find a spawn cell near the target
            if (!TryFindSpawnCell(map, target.Position, out IntVec3 spawnCell))
            {
                ModLog.Warn("[CodexPunch] Could not find a valid spawn cell near target");
                return false;
            }

            // Teleport Codex onto the map with skip visual
            SkipUtility.SkipTo(codex, spawnCell, map);

            // Assign the punch job
            Job punchJob = JobMaker.MakeJob(MSSFPDefOf.MSSFP_SkipPunchAndLeave, target);
            punchJob.ignoreForbidden = true;
            codex.jobs.StartJob(punchJob, JobCondition.InterruptForced);

            // Send the narrative letter — target the colonist, not Codex
            // (Codex will despawn after punching, so his LookTargets would be stale)
            Find.LetterStack.ReceiveLetter(
                "MSS_FP_CodexPunch_LetterLabel".Translate(),
                "MSS_FP_CodexPunch_LetterText".Translate(target.LabelShort),
                LetterDefOf.NeutralEvent,
                new LookTargets(target));

            return true;
        }

        private static bool TryFindSpawnCell(Map map, IntVec3 near, out IntVec3 result)
        {
            return CellFinder.TryFindRandomCellNear(
                near,
                map,
                MaxSpawnDistance,
                c => c.Standable(map)
                     && c.InBounds(map)
                     && !c.Fogged(map)
                     && c.GetRoom(map) != null
                     && near.DistanceTo(c) >= MinSpawnDistance,
                out result);
        }
    }
}
