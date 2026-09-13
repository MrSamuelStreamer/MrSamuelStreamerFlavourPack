using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VanillaMemesExpanded;
using Verse;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(RitualOutcomeEffectWorker_LeadershipChallenge), nameof(RitualOutcomeEffectWorker_LeadershipChallenge.Apply))]
public static class RitualOutcomeEffectWorker_LeadershipChallenge_Patch
{
    public static void Postfix(LordJob_Ritual jobRitual)
    {
        if (!BloodCourtDuelUtility.IsNonLethalLeadershipDuel(jobRitual))
            return;
        if (jobRitual is not LordJob_Ritual_Duel duel)
            return;

        WorldComponent_BloodCourtDuelCooldowns.Instance?.StartCooldown();
        ClearLeadershipObligations(jobRitual.Ritual);

        Pawn winner = BloodCourtDuelUtility.StandingWinner(duel);
        if (winner == null)
            return;

        Precept_Role role =
            Faction.OfPlayer?.ideos?.PrimaryIdeo?.GetPrecept(PreceptDefOf.IdeoRole_Leader)
            as Precept_Role;
        if (role == null)
            return;

        WorldComponent_BestMeleeLeaderTracker tracker = WorldComponent_BestMeleeLeaderTracker.Instance;
        if (tracker != null)
            tracker.currentBestMeleeLeaderPawn = winner;
        if (role.ChosenPawnSingle() != winner)
            role.Assign(winner, true);
    }

    private static void ClearLeadershipObligations(Precept_Ritual ritual)
    {
        if (ritual?.activeObligations == null)
            return;

        List<RitualObligation> toRemove = ritual.activeObligations.ToList();
        foreach (RitualObligation obligation in toRemove)
            ritual.RemoveObligation(obligation, true);
    }
}
