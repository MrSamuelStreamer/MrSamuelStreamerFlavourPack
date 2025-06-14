using RimWorld;
using Verse;

namespace MSSFP.Needs;

public class Need_GeneStealer : Need
{
    public const float FallPerDay = 1 / 6f;
    public const float FallPerInterval = 150 * (FallPerDay / GenDate.TicksPerDay);
    private const float MinAgeForNeed = 13f;

    public override float CurLevel
    {
        get
        {
            if (!MSSFPMod.settings.EnableGeneStealerNeed)
                return 1f;
            return base.CurLevel;
        }
    }

    protected override bool IsFrozen
    {
        get => pawn.ageTracker.AgeBiologicalYears < 13.0 || !MSSFPMod.settings.EnableGeneStealerNeed || base.IsFrozen;
    }

    public override bool ShowOnNeedList
    {
        get => pawn.ageTracker.AgeBiologicalYears >= 13.0 && MSSFPMod.settings.EnableGeneStealerNeed && base.ShowOnNeedList;
    }

    public Need_GeneStealer(Pawn newPawn)
        : base(newPawn)
    {
        threshPercents = [0.3f];
    }

    public override void NeedInterval()
    {
        if (IsFrozen)
            return;
        CurLevel -= FallPerInterval;

        Hediff exhaustion = pawn.health.hediffSet.GetFirstHediffOfDef(MSSFPDefOf.MSS_Need_GeneStealer_Exhaustion);
        Hediff restless = pawn.health.hediffSet.GetFirstHediffOfDef(MSSFPDefOf.MSS_Need_GeneStealer_Restless);

        switch (CurLevel)
        {
            case < 1 / 3f:
            {
                exhaustion ??= pawn.health.AddHediff(MSSFPDefOf.MSS_Need_GeneStealer_Exhaustion);

                exhaustion.Severity = CurLevel;

                if (restless != null)
                {
                    pawn.health.RemoveHediff(restless);
                }

                break;
            }
            case < 2 / 3f:
            {
                restless ??= pawn.health.AddHediff(MSSFPDefOf.MSS_Need_GeneStealer_Restless);
                restless.Severity = CurLevel;
                if (exhaustion != null)
                {
                    pawn.health.RemoveHediff(exhaustion);
                }

                break;
            }
            default:
            {
                if (restless != null)
                {
                    pawn.health.RemoveHediff(restless);
                }
                if (exhaustion != null)
                {
                    pawn.health.RemoveHediff(exhaustion);
                }

                break;
            }
        }
    }

    public void Notify_GeneGained()
    {
        CurLevel = 1f;
    }
}
