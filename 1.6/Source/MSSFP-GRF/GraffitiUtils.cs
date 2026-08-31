using Verse;

namespace MSSFP.GRF;

/// <summary>
/// Shared lookups for the graffiti compatibility patch.
/// </summary>
public static class GraffitiUtils
{
    /// <summary>Graffiti Mod's painted-graffiti def. Not ours, so it is matched by name.</summary>
    public const string GraffitiDefName = "GraffitiMod_Paint";

    private static ThingDef graffitiDef;
    private static bool resolved;

    /// <summary>
    /// Resolved lazily and cached. Deliberately NOT resolved in the <see cref="Mod"/>
    /// constructor — mod ctors run before <see cref="DefDatabase{T}"/> is populated, so an
    /// eager lookup there caches a permanent null.
    /// </summary>
    public static ThingDef GraffitiDef
    {
        get
        {
            if (resolved)
            {
                return graffitiDef;
            }

            graffitiDef = DefDatabase<ThingDef>.GetNamedSilentFail(GraffitiDefName);
            resolved = true;

            if (graffitiDef == null)
            {
                ModLog.Error(
                    $"Graffiti compatibility loaded but {GraffitiDefName} is missing; the patch will no-op."
                );
            }

            return graffitiDef;
        }
    }

    public static bool IsGraffiti(Thing t) =>
        t != null && GraffitiDef != null && t.def == GraffitiDef;
}
