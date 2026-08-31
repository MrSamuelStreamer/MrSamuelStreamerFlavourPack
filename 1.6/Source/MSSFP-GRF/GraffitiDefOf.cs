using RimWorld;
using Verse;

namespace MSSFP.GRF;

[DefOf]
public static class GraffitiDefOf
{
    /// <summary>Player-ordered single-graffiti clean. Ours, not Graffiti Mod's.</summary>
    public static JobDef MSSFP_CleanGraffitiManual;

    static GraffitiDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(GraffitiDefOf));
}
