using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Defensive logger: holos should never end up on a non-player faction. The vanilla projection
/// is bound to the projector colony. If something flips a holo's faction (raid capture quest,
/// mod interaction, etc.) we surface it as a warning so the bug is visible rather than silent.
/// Does not block — collapse-on-faction-change is handled elsewhere.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
public static class Pawn_SetFaction_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, Faction newFaction)
    {
        if (!MSSFPHoloUtil.IsHolo(__instance))
            return;
        if (newFaction == Faction.OfPlayerSilentFail)
            return;
        Log.Warning($"[MSSFP] Holo {__instance?.LabelShortCap} faction changed to {newFaction?.Name ?? "null"} (expected Player).");
    }
}
