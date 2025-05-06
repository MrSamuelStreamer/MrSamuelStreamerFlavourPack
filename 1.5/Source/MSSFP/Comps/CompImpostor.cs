using System.Text;
using Verse;

namespace MSSFP.Comps;

public class CompImpostor : ThingComp
{
    private bool isSus;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            isSus = Rand.Chance(0.2f); // 1 in 5 chance of being sus
        }
    }

    public bool IsSus => isSus;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref isSus, "isSus", false);
    }

    public override string GetDescriptionPart()
    {
        StringBuilder sb = new StringBuilder(base.GetDescriptionPart());

        if (DebugSettings.ShowDevGizmos)
        {
            sb.Append($"IsSus =? {isSus}");
        }
        return sb.ToString();
    }
}
