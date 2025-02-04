using RimWorld;
using Verse;
// ReSharper disable UnassignedReadonlyField

namespace MSSFP.VEE;

[DefOf]
public static class MSSFVEEPDefOf
{
    public static IncidentDef MSS_NuclearFallout;

    static MSSFVEEPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFVEEPDefOf));
}
