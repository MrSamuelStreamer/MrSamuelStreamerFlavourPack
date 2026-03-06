using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

public class ElevationOverlayWorldComponent : WorldComponent
{
    private bool cachedShowElevationOverlay;

    public ElevationOverlayWorldComponent(RimWorld.Planet.World world) : base(world)
    {
        cachedShowElevationOverlay = MSSFPMod.settings.ShowElevationOverlay;
    }

    public override void WorldComponentUpdate()
    {
        base.WorldComponentUpdate();
        // set dirty when the setting is toggled.
        if (cachedShowElevationOverlay != MSSFPMod.settings.ShowElevationOverlay)
        {
            cachedShowElevationOverlay = MSSFPMod.settings.ShowElevationOverlay;
            Find.World.renderer.GetLayer<WorldDrawLayer_ElevationOverlay>(Find.WorldGrid.Surface).SetDirty();
        }
    }
}
