using System.Text;
using Verse;

namespace MSSFP.Comps;

public class CompDirtHaver : ThingComp
{
    public bool hasDirt = false;
    public int wasGivenDirtAtTick = -1;

    public CompProperties_DirtHaver Props => (CompProperties_DirtHaver)props;

    public virtual bool HasDirt => hasDirt;

    public override void CompTick()
    {
        if (!(MSSFPMod.settings?.EnableDirtJobs ?? false))
            return;
        base.CompTick();
        if (!hasDirt || !(wasGivenDirtAtTick + Props.dirtExpireTime < Find.TickManager.TicksGame))
        {
            return;
        }

        hasDirt = false;
        wasGivenDirtAtTick = -1;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref hasDirt, "hasDirt");
        Scribe_Values.Look(ref wasGivenDirtAtTick, "wasGivenDirtAtTick");
    }

    public virtual void Notify_HasDirt()
    {
        hasDirt = true;
        wasGivenDirtAtTick = Find.TickManager.TicksGame;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new(base.CompInspectStringExtra());
        if (!MSSFPMod.settings.EnableDirtJobs)
            return sb.ToString();
        sb.Append(HasDirt ? "MSSFP_HasDirt".Translate() : "MSSFP_DoesNotHaveDirt".Translate());
        return sb.ToString();
    }
}
