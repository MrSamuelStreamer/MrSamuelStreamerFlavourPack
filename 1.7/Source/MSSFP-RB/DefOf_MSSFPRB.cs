using RimWorld;
using Verse;

namespace MSSFP.RB;

/// <summary>
/// Defs shipped by the Rimbody compat folder. Safe to declare here rather than on core
/// MSSFPDefOf: this assembly only loads when <c>loadFolders.xml</c> emits the compat
/// folder, so the defs are guaranteed present alongside it.
/// </summary>
[DefOf]
public static class DefOf_MSSFPRB
{
    public static JobDef MSSFP_DoPawnLift;

    public static ThoughtDef MSSFP_LiftedAsWeight;

    static DefOf_MSSFPRB()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(DefOf_MSSFPRB));
    }
}
