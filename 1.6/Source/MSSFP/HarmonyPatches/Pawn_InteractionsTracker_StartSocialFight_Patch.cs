using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot start or be drawn into social fights (pacifist projection). Blocks the
/// initiator-side <see cref="Pawn_InteractionsTracker.StartSocialFight"/> call when either
/// participant is a holo.
/// </summary>
[HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.StartSocialFight))]
public static class Pawn_InteractionsTracker_StartSocialFight_Patch
{
    private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_InteractionsTracker), "pawn");

    [HarmonyPrefix]
    public static bool Prefix(Pawn_InteractionsTracker __instance, Pawn otherPawn)
    {
        Pawn self = PawnField.GetValue(__instance) as Pawn;
        if (MSSFPHoloUtil.IsHolo(self) || MSSFPHoloUtil.IsHolo(otherPawn))
            return false;
        return true;
    }
}
