using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

public class Gene_VoidsEmbrace : Gene_Deathless
{
    public List<TraitDef> lastVoidTraits = new List<TraitDef>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref lastVoidTraits, "lastVoidTraits", LookMode.Def);
        lastVoidTraits ??= new List<TraitDef>();
    }
}
