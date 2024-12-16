using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_Haunt: HediffCompProperties
{
    public GraphicData graphicData;
    public List<Vector3> offsets;
    public bool onlyRenderWhenDrafted;

    public ThoughtDef thought;

    public HediffCompProperties_Haunt()
    {
        this.compClass = typeof(HediffComp_Haunt);
    }
}
