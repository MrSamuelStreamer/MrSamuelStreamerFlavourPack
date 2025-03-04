using RimWorld;
using Verse;
using VFETribals;

// ReSharper disable UnassignedReadonlyField

namespace MSSFP.VFE;

[DefOf]
public static class MSSFPVETDefOf
{
    public static EraAdvancementDef MSSFP_FormArchoMind;
    public static PreceptDef MSSFP_AdvanceToArcho;

    static MSSFPVETDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPVETDefOf));
}
