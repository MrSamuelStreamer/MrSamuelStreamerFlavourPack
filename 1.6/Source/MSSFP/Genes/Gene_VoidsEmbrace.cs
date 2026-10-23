using System.Collections.Generic;
using System.Linq;
using MSSFP.Defs;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

public class Gene_VoidsEmbrace : Gene_Deathless
{
    public List<TraitDef> lastVoidTraits = new List<TraitDef>();

    // Not saved: re-runs once per load so pawns cursed before a rebalance get cleaned up
    private bool bannedTraitsPurged;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref lastVoidTraits, "lastVoidTraits", LookMode.Def);
        lastVoidTraits ??= new List<TraitDef>();
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (bannedTraitsPurged) return;

        bannedTraitsPurged = true;
        PurgeBannedTraits();
    }

    // A near-death can be rare, so a void trait that is now vetoed (or has commonality 0,
    // like VFE Pirates' shellcasket) would otherwise stay on the pawn until the next one.
    private void PurgeBannedTraits()
    {
        TraitSet traits = pawn?.story?.traits;
        if (traits == null) return;

        VoidsEmbraceConfigDef config = MSSFPDefOf.MSSFP_VoidsEmbraceConfig;
        foreach (TraitDef def in lastVoidTraits.ToList())
        {
            Trait held = traits.GetTrait(def);
            if (held == null) continue;

            bool banned = config.IsVetoed(def, held.Degree)
                          || def.GetGenderSpecificCommonality(pawn.gender) <= 0f;
            if (!banned) continue;

            traits.RemoveTrait(held);
            lastVoidTraits.Remove(def);
        }
    }
}
