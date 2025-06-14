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

    public bool AlwaysOn => OnTimeTicksRange == null && OffTimeTicksRange == null;

    public ThoughtDef thought;

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

        if (OnTimeTicksRange != null && OffTimeTicksRange == null)
        {
            yield return "OffTimeTicksRange must be set if OnTimeTicksRange is set";
        }

        if (OffTimeTicksRange != null && OnTimeTicksRange == null)
        {
            yield return "OnTimeTicksRange must be set if OffTimeTicksRange is set";
        }
    }
}
