using System.Collections;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

/// <summary>
/// Global world draw layer that submits the black hole sphere mesh for rendering
/// once per frame while <see cref="GameCondition_BlackHole.IsActive"/> is true.
///
/// Instantiated by RimWorld from the MSSFP_BlackHoleDrawLayer
/// <see cref="GlobalWorldDrawLayerDef"/>.
/// </summary>
public class WorldDrawLayer_BlackHole : WorldDrawLayerBase
{
    protected override int RenderLayer => WorldCameraManager.WorldSkyboxLayer;

    public override void Render()
    {
        if (!GameCondition_BlackHole.IsActive) return;
        GameCondition_BlackHole.SubmitWorldDrawCall(RenderLayer);
    }

    public override IEnumerable Regenerate()
    {
        dirty = false;
        yield break;
    }
}
