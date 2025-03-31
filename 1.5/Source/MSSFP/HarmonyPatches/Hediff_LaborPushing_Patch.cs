using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Hediff_LaborPushing))]
public static class Hediff_LaborPushing_Patch
{
    public static Lazy<FieldInfo> debugForceBirthOutcome = new(() => AccessTools.Field(typeof(Hediff_LaborPushing), "debugForceBirthOutcome"));
    public static Lazy<FieldInfo> preventLetter = new(() => AccessTools.Field(typeof(Hediff_LaborPushing), "preventLetter"));

    [HarmonyPatch(nameof(Hediff_LaborPushing.PreRemoved))]
    [HarmonyPrefix]
    public static bool PreRemoved_Prefix(Hediff_LaborPushing __instance)
    {
        ModLog.Debug("Running Patched Hediff_LaborPushing.PreRemoved");
        if (!RitualOutcomeEffectWorker_ChildBirth_Patch.CheckConcievedInUpgradableBed(__instance.pawn, out _, out _))
        {
            return true;
        }

        ModLog.Debug("Running Patched ApplyBirthOutcome");

        Find.WorldPawns.RemovePreservedPawnHediff(__instance.Mother, __instance);
        Find.WorldPawns.RemovePreservedPawnHediff(__instance.Father, __instance);

        LordJob_Ritual lordJob = __instance.pawn.GetLord()?.LordJob as LordJob_Ritual;
        Precept_Ritual precept = (Precept_Ritual)__instance.pawn.Ideo.GetPrecept(PreceptDefOf.ChildBirth);
        if (lordJob?.Ritual == null || lordJob.Ritual.def != PreceptDefOf.ChildBirth || lordJob.assignments.FirstAssignedPawn("mother") != __instance.pawn)
        {
            float birthQualityFor = PregnancyUtility.GetBirthQualityFor(__instance.pawn);
            RitualOutcomePossibility outcome =
                ((RitualOutcomePossibility)debugForceBirthOutcome.Value.GetValue(__instance))
                ?? ((RitualOutcomeEffectWorker_FromQuality)precept.outcomeEffect).GetOutcome(birthQualityFor, null);
            RitualRoleAssignments ritualRoleAssignments = PregnancyUtility.RitualAssignmentsForBirth(precept, __instance.pawn);
            double quality = birthQualityFor;
            Precept_Ritual ritual = precept;
            List<GeneDef> genesListForReading = __instance.geneSet?.GenesListForReading;
            Pawn geneticMother = __instance.Mother ?? __instance.pawn;
            Pawn pawn = __instance.pawn;
            Pawn father = __instance.Father;
            RitualRoleAssignments assignments = ritualRoleAssignments;
            RitualOutcomeEffectWorker_ChildBirth_Patch.ApplyBirthOutcome(
                outcome,
                (float)quality,
                ritual,
                genesListForReading,
                geneticMother,
                pawn,
                father,
                null,
                lordJob,
                assignments
            );
        }
        else
            lordJob?.ApplyOutcome(1f);

        return false;
    }
}
