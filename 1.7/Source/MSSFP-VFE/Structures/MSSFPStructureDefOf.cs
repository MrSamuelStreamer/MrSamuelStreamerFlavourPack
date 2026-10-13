using RimWorld;
using RimWorld.Planet;
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

    /// <summary>The SitePart that hosts standalone (whole-map) structures on a Point of Interest.</summary>
    public static readonly SitePartDef MSS_PointOfInterest;

    /// <summary>WorldObjectDef passed to SiteMaker.MakeSite for a Point of Interest.</summary>
    public static readonly WorldObjectDef MSS_PointOfInterestWorldObject;

    static MSSFPStructureDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPStructureDefOf));
}
