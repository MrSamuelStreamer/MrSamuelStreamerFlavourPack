using HarmonyLib;
using MSSFP.Haunts;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// When a pawn is resurrected (via any path — serum, ability, death refusal, shambler),
/// remove any haunts that reference them as the spirit. They're alive now.
/// All resurrection paths funnel through ResurrectionUtility.TryResurrect.
/// </summary>
[HarmonyPatch(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect))]
public static class Resurrect_ClearHaunts_Patch
{
    public static void Postfix(Pawn pawn, bool __result)
    {
        if (!__result)
            return;

        HauntCleanupUtility.RemoveHauntsForSpirit(pawn);
    }
}
