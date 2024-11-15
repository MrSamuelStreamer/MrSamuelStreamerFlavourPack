using RimWorld;
using Verse;

namespace MSSFP;

[DefOf]
public static class MSSFPDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    // public static GeneDef YourPrefix_YourGeneDefName;

    static MSSFPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDefOf));
}
