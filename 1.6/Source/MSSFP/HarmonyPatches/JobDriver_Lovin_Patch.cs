using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot participate in lovin' — no physical body, no pregnancy chance, no bed claim.
/// Prefix on <see cref="JobDriver_Lovin.TryMakePreToilReservations"/> fails the reservation
/// if either the initiator pawn (public <c>JobDriver.pawn</c>) or the <c>Partner</c>
/// (private property, resolved via reflection) is a holo.
/// </summary>
[HarmonyPatch(typeof(JobDriver_Lovin), nameof(JobDriver_Lovin.TryMakePreToilReservations))]
public static class JobDriver_Lovin_Patch
{
    private static readonly PropertyInfo PartnerProp = AccessTools.Property(typeof(JobDriver_Lovin), "Partner");

    [HarmonyPrefix]
    public static bool Prefix(JobDriver_Lovin __instance, ref bool __result)
    {
        Pawn self = __instance.pawn;
        Pawn partner = PartnerProp?.GetValue(__instance) as Pawn;
        if (!MSSFPHoloUtil.IsHolo(self) && !MSSFPHoloUtil.IsHolo(partner))
            return true;
        __result = false;
        return false;
    }
}
