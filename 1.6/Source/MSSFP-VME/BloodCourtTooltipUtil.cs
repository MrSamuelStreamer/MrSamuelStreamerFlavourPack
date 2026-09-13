using RimWorld;
using Verse;

namespace MSSFP.VME;

public static class BloodCourtTooltipUtil
{
    public const string OutcomeDefName = "VME_LeadershipChallengeRitualOutcome";

    private static string originalDescription;

    public static void Apply()
    {
        RitualOutcomeEffectDef def = DefDatabase<RitualOutcomeEffectDef>.GetNamedSilentFail(OutcomeDefName);
        if (def?.extraPredictedOutcomeDescriptions == null || def.extraPredictedOutcomeDescriptions.Count == 0)
        {
            ModLog.Warn($"Could not update Blood Court outcome tooltip — {OutcomeDefName} not found.");
            return;
        }

        originalDescription ??= def.extraPredictedOutcomeDescriptions[0];
        def.extraPredictedOutcomeDescriptions[0] = BloodCourtDuelUtility.Enabled
            ? "MSS_FP_BloodCourt_NonLethalOutcomeDesc".Translate().Resolve()
            : originalDescription;
    }
}
