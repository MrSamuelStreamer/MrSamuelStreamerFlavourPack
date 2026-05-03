using MSSFP.MapComponents;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// 3x6 outdoor sun tower. While active, drives a Harmony postfix on
/// <see cref="Verse.SkyManager.CurrentSkyTarget"/> that clamps the map's sky glow
/// to a 0.85 minimum. Clamp is suppressed when an UnnaturalDarkness / DarkenedSkies
/// (Anomaly) condition is active — see <see cref="MSSFPSunTowerMapComponent.IsForcedDark"/>.
///
/// Power gating is delegated to vanilla <see cref="CompPowerTrader"/> which already
/// composites Flick / Schedule / Breakdown / WantsToBeOn into a single PowerOn signal.
/// We only listen to PowerTurnedOn / PowerTurnedOff. Solar flare zeroes power → tower
/// deactivates naturally. Eclipse (vanilla GameCondition_NoSunlight) darkens via
/// LerpDarken which runs BEFORE the postfix, so the tower wins eclipse — intentional.
///
/// Roof-after-build is handled by a per-TickRare check of the building's OccupiedRect
/// against <see cref="RoofGrid"/>; if any cell is roofed the tower deactivates without
/// shutting down power.
///
/// The <see cref="active"/> field is intentionally NOT scribed — it is recomputed
/// from PowerOn / Spawned / roof state on first TickRare/SpawnSetup after load.
/// </summary>
public class Building_SunTower : Building
{
    /// <summary>True if powered, spawned, and unroofed. Read by the SkyManager postfix
    /// (indirectly via <see cref="MSSFPSunTowerMapComponent.AnyActive"/>) to decide
    /// whether to clamp glow this frame.</summary>
    public bool active;

    private CompPowerTrader powerComp;
    private Graphic graphicPowered;
    private Graphic graphicUnpowered;

    public override void SpawnSetup(Verse.Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        ResolveGraphicsCache();
        // Authoritative initial sync — populates anyActive cache before first render frame.
        RecomputeActive();
        Map?.GetComponent<MSSFPSunTowerMapComponent>()?.Notify_ActiveStateChanged();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Verse.Map oldMap = Map;
        active = false;
        base.DeSpawn(mode);
        // Notify after base.DeSpawn so listerThings reflects the removal.
        oldMap?.GetComponent<MSSFPSunTowerMapComponent>()?.Notify_ActiveStateChanged();
    }

    protected override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        // CompPowerTrader collapses Flicked/Scheduled/Broken/WantsToBeOn → Power signals.
        if (signal == "PowerTurnedOn" || signal == "PowerTurnedOff")
        {
            RecomputeActive();
            Map?.GetComponent<MSSFPSunTowerMapComponent>()?.Notify_ActiveStateChanged();
        }
    }

    public override void TickRare()
    {
        base.TickRare();
        // Safety-net + roof-after-build detection (no signal source for roof changes).
        bool prev = active;
        RecomputeActive();
        if (prev != active)
        {
            Map?.GetComponent<MSSFPSunTowerMapComponent>()?.Notify_ActiveStateChanged();
        }
    }

    /// <summary>True if this tower is powered, unroofed, spawned. Cached in <see cref="active"/>.</summary>
    public bool IsContributingLight => active;

    public override Graphic Graphic
    {
        get
        {
            // Cache may not yet be resolved during early spawn-setup paths; fall back.
            if (graphicPowered == null || graphicUnpowered == null)
            {
                return base.Graphic;
            }
            bool poweredAndUnroofed = powerComp != null && powerComp.PowerOn && !AnyOccupiedCellRoofed();
            return poweredAndUnroofed ? graphicPowered : graphicUnpowered;
        }
    }

    /// <summary>
    /// Recomputes <see cref="active"/> from live state (PowerOn + Spawned + !roofed).
    /// Returns the new value. Called by SpawnSetup, ReceiveCompSignal, TickRare, and
    /// by the map component during multi-tower aggregation to avoid stale-cache bugs
    /// when multiple towers' states change in the same tick.
    /// </summary>
    public bool RecomputeActive()
    {
        active = powerComp != null && powerComp.PowerOn && Spawned && !AnyOccupiedCellRoofed();
        return active;
    }

    private bool AnyOccupiedCellRoofed()
    {
        if (!Spawned)
        {
            return false;
        }
        Verse.Map m = Map;
        foreach (IntVec3 c in this.OccupiedRect())
        {
            if (m.roofGrid.Roofed(c))
            {
                return true;
            }
        }
        return false;
    }

    private void ResolveGraphicsCache()
    {
        if (def?.graphicData == null)
        {
            return;
        }
        graphicPowered = def.graphic;
        // Unpowered graphic shares drawSize / shader with powered, only path differs.
        graphicUnpowered = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/MSS_SunTower_Unpowered",
            def.graphic.Shader,
            def.graphicData.drawSize,
            Color.white
        );
    }
}
