using HarmonyLib;
using MSSFP.Compatibility.BigAndSmall;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="Pawn.SpawnSetup"/> that repairs humanlike trackers on MSSFP
/// sapient ravens whenever they spawn — including the <c>respawningAfterLoad</c> path
/// when a save authored before the in-incident repair shipped is reloaded.
///
/// Gated on <c>__instance.def.defName == "HL_MSSFP_Raven"</c> so the postfix is a single
/// string compare for every other pawn — measurable but trivial. The <c>HL_</c> prefix is
/// reserved by B&amp;S for its generated humanlike-animal ThingDefs; checking the exact
/// MSSFP-specific defName keeps the scope narrow and avoids touching other mods'
/// sapient-animal pawns (their authors are responsible for their own tracker shape).
///
/// See <see cref="SapientRaven_TrackerRepair"/> for the cascade analysis and the list of
/// trackers being repaired.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
public static class Pawn_SpawnSetup_SapientRavenRepair
{
    private const string SapientRavenDefName = "HL_MSSFP_Raven";

    public static void Postfix(Pawn __instance)
    {
        if (__instance?.def == null) return;
        if (__instance.def.defName != SapientRavenDefName) return;
        SapientRaven_TrackerRepair.EnsureHumanlikeTrackers(__instance);
    }
}
