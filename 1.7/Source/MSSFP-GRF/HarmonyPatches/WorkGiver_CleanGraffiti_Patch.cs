using System.Reflection;
using HarmonyLib;
using Verse;

namespace MSSFP.GRF.HarmonyPatches;

/// <summary>
/// Stops Graffiti Mod's own cleaning WorkGiver handing out jobs. This is the only route by
/// which graffiti is ever removed on its own.
///
/// Its thingClass is named <c>Filth_Graffiti</c> and its def carries
/// <c>&lt;category&gt;Filth&lt;/category&gt;</c> plus a full <c>&lt;filth&gt;</c> props block, all of
/// which suggests the vanilla filth systems also act on it. Verified in-game: they do not.
/// <c>Filth_Graffiti</c> derives from <c>Building_Art</c>, not <c>Filth</c>, so
/// <c>WorkGiver_CleanFilth</c>'s <c>t as Filth</c> never matches, it never enters
/// <c>ListerFilthInHomeArea</c>, and <c>SteadyEnvironmentEffects</c> skips its filth branch
/// entirely — making the def's <c>rainWashes</c> decorative. Deterioration does not apply
/// either (<c>useHitPoints</c> is false, so <c>CanEverDeteriorate</c> is false).
/// Patching anything on <c>Filth</c> for graffiti is therefore dead code; do not add it back.
///
/// Bound by string rather than <c>typeof</c> for two reasons. Its
/// <c>WorkGiver_CleanGraffiti</c> is <c>internal</c>, so it is not nameable from here at
/// compile time; and a <c>typeof</c> against a foreign assembly resolves during
/// <c>PatchAll()</c>, which would throw — losing every patch in this assembly, not just
/// this one — if mod ordering ever put us ahead of GraffitiMod.dll.
///
/// <c>Prepare</c> returning false is the graceful path if upstream renames the class.
/// </summary>
[HarmonyPatch]
public static class WorkGiver_CleanGraffiti_Patch
{
    private const string TargetName = "GraffitiMod.WorkGiver_CleanGraffiti:HasJobOnThing";

    public static MethodBase TargetMethod() => AccessTools.Method(TargetName);

    public static bool Prepare()
    {
        if (TargetMethod() != null)
        {
            return true;
        }

        ModLog.Error($"Could not resolve {TargetName}; graffiti cleaning will not be blocked.");
        return false;
    }

    [HarmonyPostfix]
    public static void Postfix(Thing t, ref bool __result)
    {
        if (__result && GraffitiSettingsTab.Permanent && GraffitiUtils.IsGraffiti(t))
        {
            __result = false;
        }
    }
}
