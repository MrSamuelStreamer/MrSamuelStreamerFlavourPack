using RimWorld;
using Verse;

namespace Mr_Samuel_Streamer_Flavour_Pack;

[DefOf]
public static class Mr_Samuel_Streamer_Flavour_PackDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    // public static GeneDef YourPrefix_YourGeneDefName;
    
    static Mr_Samuel_Streamer_Flavour_PackDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Mr_Samuel_Streamer_Flavour_PackDefOf));
}
