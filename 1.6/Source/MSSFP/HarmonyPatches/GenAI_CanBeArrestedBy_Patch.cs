using HarmonyLib;
using MSSFP.Holo;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot be arrested. Patches the <see cref="GenAI.CanBeArrestedBy"/> extension method
/// (the actual vanilla gating used by <c>FloatMenuOptionProvider_Arrest</c> and
/// <c>JobDriver_TakeToBed</c>).
/// </summary>
[HarmonyPatch(typeof(GenAI), nameof(GenAI.CanBeArrestedBy))]
public static class GenAI_CanBeArrestedBy_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref bool __result)
    {
        if (MSSFPHoloUtil.IsHolo(pawn))
            __result = false;
    }
}
