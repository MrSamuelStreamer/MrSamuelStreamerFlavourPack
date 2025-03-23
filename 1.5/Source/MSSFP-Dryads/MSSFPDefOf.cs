using MSSFP.Genes;
using MSSFP.Thoughts;
using RimWorld;
using Verse;

// ReSharper disable UnassignedReadonlyField

namespace MSSFP.Dryads;

[DefOf]
public static class MSSFPDryadDefOf
{
    public static readonly ThingDef MSSFP_Plant_TreeFroganlen;
    public static readonly IncidentDef MSS_FroggomancerRescue;

    static MSSFPDryadDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDryadDefOf));
}
