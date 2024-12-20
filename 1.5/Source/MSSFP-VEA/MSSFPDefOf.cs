using RimWorld;
using Verse;

namespace MSSFP.VAE;

[DefOf]
public static class MSSFPVAEDefOf
{
    public static readonly TraitDef MSS_SweetBabyBoy;

    static MSSFPVAEDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPVAEDefOf));
}
