using HarmonyLib;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(MSSFPMod), nameof(MSSFPMod.WriteSettings))]
public static class MSSFPMod_WriteSettings_Patch
{
    public static void Postfix() => BloodCourtTooltipUtil.Apply();
}
