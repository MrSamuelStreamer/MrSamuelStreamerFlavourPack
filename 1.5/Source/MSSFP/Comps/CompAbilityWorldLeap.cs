using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps;

public class CompAbilityWorldLeap: CompAbilityEffect_Farskip
{
    public CompProperties_AbilityWorldLeap Props => (CompProperties_AbilityWorldLeap) this.props;


    public override bool ShouldHideGizmo
    {
        get
        {
            return !MSSFPDefOf.MSS_FroggeLeapResearch.IsFinished;
        }
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (base.GizmoDisabled(out reason)) return true;

        if (!MSSFPDefOf.MSS_FroggeLeapResearch.IsFinished)
        {
            return false;
        }
        if (parent.pawn.GetCaravan() == null)
        {
            reason = "Not in a caravan";
            return true;
        }

        return false;
    }

    public bool HasEnoughGems(out float gems)
    {
        gems = parent.pawn.inventory.innerContainer.InnerListForReading.Where(t=>Props.EdibleGems.Contains(t.def)).Sum(t=>t.stackCount * t.MarketValue);

        return gems > Props.ValueRequired;
    }

    public virtual void ConsumeGems()
    {
        float gems = Props.ValueRequired;

        foreach (Thing thing in parent.pawn.inventory.innerContainer.InnerListForReading.Where(t=>Props.EdibleGems.Contains(t.def)))
        {
            for (int i = 0; i < thing.stackCount; i++)
            {
                Thing newThing = thing.SplitOff(1);
                gems -= newThing.MarketValue;
                newThing.Destroy();
                if(gems <= 0) return;
            }
        }
    }


    public override void Apply(GlobalTargetInfo target)
    {
        base.Apply(target);
        ConsumeGems();
    }

    public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
    {
        Caravan caravan = parent.pawn.GetCaravan();
        if (caravan is { ImmobilizedByMass: true })
            return false;

        if (Find.World.Impassable(target.Tile)) return false;

        return Find.WorldGrid.ApproxDistanceInTiles(target.Tile, caravan.Tile) < Props.LeapRange && HasEnoughGems(out float _);
    }

    public override string WorldMapExtraLabel(GlobalTargetInfo target)
    {
        Caravan caravan = parent.pawn.GetCaravan();
        if (caravan != null && caravan.ImmobilizedByMass)
            return "CaravanImmobilizedByMass".Translate();
        if(!HasEnoughGems(out float gems))
            return "MSSFP_LeapNoFuel".Translate(gems, Props.ValueRequired);
        if (!Valid(target, false))
            return "MSSFP_LeapTooFar".Translate(Props.LeapRange);
        return "MSSFP_LeapTo";
    }
}
