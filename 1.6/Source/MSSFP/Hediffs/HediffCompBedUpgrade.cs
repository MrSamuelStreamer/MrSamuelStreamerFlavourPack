using System.Linq;
using System.Text;
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

    public override string CompDescriptionExtra
    {
        get
        {
            StringBuilder sb = new();

            foreach (
                BedUpgradeDef def in DefDatabase<BedUpgradeDef>.AllDefs.Where(def =>
                    def.stat != null && !def.appliesDirectToBed
                )
            )
            {
                if (CompUpgradableBed.StatMultipliers.TryGetValue(def.stat, out float mult))
                {
                    sb.Append(def.stat.LabelCap);
                    sb.Append(": x");
                    sb.Append(mult.ToStringPercent());
                    sb.Append("\n");
                }
                if (CompUpgradableBed.StatOffsets.TryGetValue(def.stat, out float offset))
                {
                    sb.Append(def.stat.LabelCap);
                    sb.Append(": +");
                    sb.Append(offset.ToStringPercent());
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }
    }
}
