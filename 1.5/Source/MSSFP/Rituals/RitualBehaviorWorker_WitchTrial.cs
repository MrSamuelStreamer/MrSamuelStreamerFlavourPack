using RimWorld;
using Verse;

namespace MSSFP.Rituals;

public class RitualBehaviorWorker_WitchTrial: RitualBehaviorWorker
{
    private int ticksSinceLastInteraction = -1;
    public const int SocialInteractionIntervalTicks = 700;

    public RitualBehaviorWorker_WitchTrial()
    {
    }

    public RitualBehaviorWorker_WitchTrial(RitualBehaviorDef def)
        : base(def)
    {
    }

    public override void Cleanup(LordJob_Ritual ritual)
    {
        Pawn pawn = ritual.PawnWithRole("convict");
        if (!pawn.IsPrisonerOfColony)
            return;
        pawn.guest.WaitInsteadOfEscapingFor(2500);
    }

    public override void PostCleanup(LordJob_Ritual ritual)
    {
        Pawn warden = ritual.PawnWithRole("leader");
        Pawn prisoner = ritual.PawnWithRole("convict");
        if (!prisoner.IsPrisonerOfColony)
            return;
        WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(prisoner, warden);
        prisoner.guest.WaitInsteadOfEscapingFor(1250);
    }

    public override void Tick(LordJob_Ritual ritual)
    {
        base.Tick(ritual);
        if (ritual.StageIndex == 0)
            return;
        if (ticksSinceLastInteraction == -1 || ticksSinceLastInteraction > 700)
        {
            ticksSinceLastInteraction = 0;
            Pawn recipient1 = ritual.PawnWithRole("leader");
            Pawn recipient2 = ritual.PawnWithRole("convict");
            if (Rand.Bool)
                recipient1.interactions.TryInteractWith(recipient2, InteractionDefOf.Trial_Accuse);
            else
                recipient2.interactions.TryInteractWith(recipient1, InteractionDefOf.Trial_Defend);
        }
        else
            ++ticksSinceLastInteraction;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksSinceLastInteraction, "ticksSinceLastInteraction", -1);
    }

}
