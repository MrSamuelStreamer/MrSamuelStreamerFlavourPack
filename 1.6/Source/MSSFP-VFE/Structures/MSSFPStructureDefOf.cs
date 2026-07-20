using RimWorld;
using Verse;

// ReSharper disable UnassignedReadonlyField

namespace MSSFP.VFE.Structures;

[DefOf]
public static class MSSFPStructureDefOf
{
    /// <summary>Our replacement for Base_Faction when a settlement rolls a viewer structure.</summary>
    public static readonly MapGeneratorDef MSSFP_ViewerStructureSettlement;

    /// <summary>Vanilla faction-base generator; the only result we are willing to override.</summary>
    public static readonly MapGeneratorDef Base_Faction;

    static MSSFPStructureDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPStructureDefOf));
}
