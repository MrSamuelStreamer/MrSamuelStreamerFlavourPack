using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_Haunted: CompProperties
{
    public GraphicData graphicData;
    public List<Vector3> offsets;
    public bool onlyRenderWhenDrafted;

    public CompProperties_Haunted()
    {
        compClass = typeof(CompHaunted);
    }
}
