using HarmonyLib;
using RimWorld;
using Verse;
using VFETribals;

namespace MSSFP.VET;

[HarmonyPatch(typeof(GameComponent_Tribals))]
public static class GameComponent_Tribals_Patch
{
    public static bool HaveInjectedVal = false;

    [HarmonyPatch(nameof(GameComponent_Tribals.TryRegisterAdvancementObligation))]
    [HarmonyPrefix]
    public static void TryRegisterAdvancementObligation_Prefix()
    {
        if (HaveInjectedVal)
            return;

        PreceptDef def = DefDatabase<PreceptDef>.GetNamed("MSSFP_AdvanceToArcho");
        if (def == null)
        {
            ModLog.Warn("Couldn't find MSSFP_AdvanceToArcho");
        }
        else
        {
            VFETribals.Utils.advancementPrecepts.Add(
                DefDatabase<PreceptDef>.GetNamed("MSSFP_AdvanceToArcho")
            );
        }
    }
}
