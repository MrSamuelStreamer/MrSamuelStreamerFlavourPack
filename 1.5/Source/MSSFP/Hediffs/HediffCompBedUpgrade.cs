using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompBedUpgrade : HediffComp
{
    public HediffCompProperties_BedUpgrade Props => (HediffCompProperties_BedUpgrade)props;

    public Building_Bed hediffGiver;

    public CompUpgradableBed CompUpgradableBed => hediffGiver.TryGetComp<CompUpgradableBed>();

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_References.Look(ref hediffGiver, "hediffGiver");
    }
}
