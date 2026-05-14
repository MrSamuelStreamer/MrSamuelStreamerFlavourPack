using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// AI core building. Swaps to an offline sprite while <see cref="CompPowerTrader.PowerOn"/>
/// is false. Mirrors the <see cref="Building_SunTower"/> graphic-swap pattern (graphic cache
/// resolved in SpawnSetup, getter falls back to base while cache unresolved).
///
/// Sprite swap is driven entirely by the live PowerOn signal — no state of our own to
/// scribe. Map mesh dirtying on power transitions is handled by vanilla
/// <see cref="CompPowerTrader"/> (PowerTurnedOn / PowerTurnedOff signals trigger
/// <see cref="Thing.DirtyMapMesh"/> through the parent thing's notify path).
///
/// Projection / persona lifecycle stays on <see cref="MSSFP.Holo.CompHoloProjector"/> and
/// <see cref="MSSFP.Comps.CompTrueAICore"/>. This subclass exists solely for the graphic
/// override.
/// </summary>
public class Building_AICore : Building
{
    private CompPowerTrader powerComp;
    private Graphic graphicPowered;
    private Graphic graphicOffline;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        ResolveGraphicsCache();
    }

    protected override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        // Force a redraw on power edges. CompPowerTrader already dirties the map mesh
        // via its own notify path, but DirtyMapMesh is idempotent and cheap — belt + suspenders
        // against any future code path that emits these signals without the dirty.
        if (signal == "PowerTurnedOn" || signal == "PowerTurnedOff")
        {
            if (Spawned)
            {
                DirtyMapMesh(Map);
            }
        }
    }

    public override Graphic Graphic
    {
        get
        {
            if (graphicPowered == null || graphicOffline == null)
            {
                return base.Graphic;
            }
            bool powered = powerComp == null || powerComp.PowerOn;
            return powered ? graphicPowered : graphicOffline;
        }
    }

    private void ResolveGraphicsCache()
    {
        if (def?.graphicData == null)
        {
            return;
        }
        graphicPowered = def.graphic;
        graphicOffline = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/MSSFP_AI_Core_Offline",
            def.graphic.Shader,
            def.graphicData.drawSize,
            Color.white
        );
    }
}
