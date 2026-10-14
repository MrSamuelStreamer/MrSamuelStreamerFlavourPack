using RimWorld;
using VanillaMemesExpanded;
using Verse;
using Verse.AI.Group;

namespace MSSFP.VME;

public static class BloodCourtDuelUtility
{
    public const string LeadershipChallengePreceptDefName = "VME_LeadershipChallengePrecept";
    public const string LeadershipChallengePatternDefName = "VME_LeadershipChallengeRitual";
    public const string LeadershipChallengeBehaviorDefName = "VME_LeadershipChallengeRitualBehaviour";
    public const string LeadershipChallengeOutcomeDefName = "VME_LeadershipChallengeRitualOutcome";

    public static bool Enabled => MSSFPMod.settings?.EnableNonLethalBloodCourtDuels ?? true;

    // VME versions expose this ritual under different identifiers (precept, pattern, behavior, outcome, worker).
    public static bool IsLeadershipChallenge(Precept_Ritual precept)
    {
        if (precept == null)
            return false;
        if (precept.def?.defName == LeadershipChallengePreceptDefName)
            return true;
        if (precept.def?.ritualPatternBase?.defName == LeadershipChallengePatternDefName)
            return true;
        if (precept.behavior?.def?.defName == LeadershipChallengeBehaviorDefName)
            return true;
        if (precept.outcomeEffect?.def?.defName == LeadershipChallengeOutcomeDefName)
            return true;
        return precept.outcomeEffect is RitualOutcomeEffectWorker_LeadershipChallenge;
    }

    public static bool IsLeadershipChallenge(LordJob_Ritual ritual) =>
        IsLeadershipChallenge(ritual?.Ritual);

    public static bool IsNonLethalLeadershipDuel(LordJob_Ritual ritual) =>
        Enabled && IsLeadershipChallenge(ritual);

    public static bool IsNonLethalLeadershipDuelist(Pawn pawn)
    {
        if (!Enabled || pawn == null)
            return false;

        return pawn.GetLord()?.LordJob is LordJob_Ritual_Duel duel
            && IsNonLethalLeadershipDuel(duel)
            && duel.duelists.Contains(pawn);
    }

    public static Pawn StandingWinner(LordJob_Ritual_Duel duel)
    {
        Pawn winner = null;
        foreach (Pawn pawn in duel.duelists)
        {
            if (pawn is not { Dead: false, Downed: false })
                continue;
            if (winner != null)
                return null;
            winner = pawn;
        }

        return winner;
    }
}
