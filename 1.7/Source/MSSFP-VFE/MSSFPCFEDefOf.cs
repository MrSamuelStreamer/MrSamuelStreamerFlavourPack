using RimWorld;
using Verse;

// ReSharper disable UnassignedReadonlyField

namespace MSSFP.VFE;

[DefOf]
public static class MSSFPCFEDefOf
{
    public static readonly SoundDef MSSFP_Squelch;

    static MSSFPCFEDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPCFEDefOf));
}
