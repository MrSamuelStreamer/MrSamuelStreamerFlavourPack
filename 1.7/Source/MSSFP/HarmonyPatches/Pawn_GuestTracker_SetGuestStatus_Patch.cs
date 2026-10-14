using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot be made prisoner or slave. Guest tracker exists only because vanilla
/// auto-adds it on humanlikes, but the projection has no real legal status.
///
/// Prefix on <see cref="Pawn_GuestTracker.SetGuestStatus"/> blocks Prisoner / Slave
/// transitions on holo pawns. The <c>newHost == null</c> path (used by
/// <see cref="Pawn.SetFaction"/> to normalise guest state when joining the player
/// faction) is allowed through — otherwise re-anchoring a holo to Player faction would
/// silently fail, leaving the pawn in a stale guest state.
/// </summary>
[HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
public static class Pawn_GuestTracker_SetGuestStatus_Patch
{
    private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_GuestTracker), "pawn");

    [HarmonyPrefix]
    public static bool Prefix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus)
    {
        Pawn pawn = PawnField.GetValue(__instance) as Pawn;
        if (!MSSFPHoloUtil.IsHolo(pawn))
            return true;
        // Allow un-guest / faction-normalise path.
        if (newHost == null)
            return true;
        // Block prisoner / slave transitions only.
        return guestStatus != GuestStatus.Prisoner && guestStatus != GuestStatus.Slave;
    }
}
