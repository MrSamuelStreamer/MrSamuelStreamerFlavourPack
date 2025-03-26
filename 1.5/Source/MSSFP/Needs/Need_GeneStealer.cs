using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Needs;

public class Need_GeneStealer : Need
{
    public const float FallPerDay = 1 / 6f;
    private const float MinAgeForNeed = 13f;

    protected override bool IsFrozen
    {
        get => pawn.ageTracker.AgeBiologicalYears < 13.0 || base.IsFrozen;
    }

    public override bool ShowOnNeedList
    {
        get => pawn.ageTracker.AgeBiologicalYears >= 13.0 && base.ShowOnNeedList;
    }

    public Need_GeneStealer(Pawn newPawn)
        : base(newPawn)
    {
        threshPercents = new List<float> { 0.3f };
    }

    public override void NeedInterval()
    {
        if (IsFrozen)
            return;
        CurLevel -= 8.333333E-05f;
    }

    public void Notify_GeneGained()
    {
        CurLevel = 1f;
    }
}
