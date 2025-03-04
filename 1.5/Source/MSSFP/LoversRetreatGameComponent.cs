using Verse;

namespace MSSFP;

public class LoversRetreatGameomponent : GameComponent
{
    public bool LoversRetreatEnabled = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LoversRetreatEnabled, "LoversRetreatEnabled", true);
    }
}
