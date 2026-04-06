using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_Haunt : HediffCompProperties
{
    public GraphicData graphicData;
    public List<Vector3> offsets;
    public bool onlyRenderWhenDrafted;

    public IntRange OnTimeTicksRange;
    public IntRange OffTimeTicksRange;

    public bool CanTransferInProximity = false;
    public float ProximityTransferChancePerCheck = 0.1f;
    public int ProximityTransferCheckTicks = 60000;
    public float ProximityRadius = 6;

    public bool AlwaysOn => OnTimeTicksRange == default && OffTimeTicksRange == default;

    public ThoughtDef thought;

    /// <summary>
    /// Optional color tint applied to the haunt graphic at render time.
    /// Null = no tint (use graphic's default color).
    /// </summary>
    public Color? colorOverride = null;

    /// <summary>
    /// FleckDef to spawn as ambient particles while this haunt is visible.
    /// Spawned once per render call when the haunt is in Presence or Awakened stage.
    /// Null = no particles.
    /// </summary>
    public FleckDef ambientFleck = null;

    /// <summary>How often (in ticks) to spawn ambient flecks. Default ~3 seconds.</summary>
    public int fleckIntervalTicks = 75;

    /// <summary>Whether this haunt drifts lazily around its anchor point.</summary>
    public bool enableWander = true;

    /// <summary>Maximum XZ drift radius from anchor (tiles).</summary>
    public float wanderRadius = 0.4f;

    /// <summary>Random wander push magnitude (tiles/s², XZ only).</summary>
    public float wanderAcceleration = 0.3f;

    /// <summary>Spring pull strength when outside wanderRadius (tiles/s² per tile of overshoot).</summary>
    public float catchupStrength = 2.5f;

    /// <summary>
    /// Fraction of velocity retained per second — frame-rate independent via Mathf.Pow.
    /// 0.15 = heavy drag (snappy); 0.9 = light drag (floaty).
    /// </summary>
    public float dampingPerSecond = 0.15f;

    public HediffCompProperties_Haunt()
    {
        this.compClass = typeof(HediffComp_Haunt);
    }

    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }

        if (OnTimeTicksRange != default && OffTimeTicksRange == default)
        {
            yield return "OffTimeTicksRange must be set if OnTimeTicksRange is set";
        }

        if (OffTimeTicksRange != default && OnTimeTicksRange == default)
        {
            yield return "OnTimeTicksRange must be set if OffTimeTicksRange is set";
        }
    }
}
