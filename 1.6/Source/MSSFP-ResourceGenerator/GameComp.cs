using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class GameComp(Game game) : GameComponent
{
    public Game game = game;

    public override void FinalizeInit()
    {
        MSSFPResourceGeneratorMod.UpdateExtras();
    }

    public override void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            MSSFPResourceGeneratorMod.UpdateExtras();
        }
    }
}
