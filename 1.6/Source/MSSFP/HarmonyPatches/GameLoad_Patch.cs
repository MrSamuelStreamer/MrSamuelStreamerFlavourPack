using HarmonyLib;
using MSSFP.Hediffs;
using Verse;
using PawnGraphicUtils = MSSFP.Utils.PawnGraphicUtils;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Game))]
public static class GameLoad_Patch
{
    [HarmonyPatch("LoadGame")]
    [HarmonyPrefix]
    public static void LoadGame_Prefix()
    {
        HauntsCache.Clear();
    }

    [HarmonyPatch("FinalizeInit")]
    [HarmonyPostfix]
    public static void FinalizeInit_Postfix()
    {
        PawnGraphicUtils.CleanupOrphanedTextures();
    }
}
