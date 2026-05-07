using HarmonyLib;
using MSSFP.AICore;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Drives <see cref="AICoreBubbler.OnGUI"/> from the vanilla map UI render path.
///
/// Hook target: <see cref="MapInterface.MapInterfaceOnGUI_BeforeMainTabs"/> — runs once per UI frame
/// on the main thread, before main tabs draw. Picked over <c>OnGUI_AfterMainTabs</c> so bubbles render
/// behind any open tab window rather than over it.
///
/// Self-disable contract: any exception inside the bubbler flips <see cref="AICoreBubbler.Disabled"/>
/// permanently for this session. We never re-throw — bubble breakage MUST NOT crash vanilla UI.
/// </summary>
[HarmonyPatch(typeof(MapInterface))]
public static class Patch_MapInterfaceOnGUI
{
    [HarmonyPatch(nameof(MapInterface.MapInterfaceOnGUI_BeforeMainTabs))]
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (AICoreBubbler.Disabled) return;
        try
        {
            AICoreBubbler.OnGUI();
        }
        catch (System.Exception e)
        {
            Log.ErrorOnce(
                $"[MSSFP] AICoreBubbler.OnGUI threw — disabling bubble renderer for this session. {e}",
                0x4F1B7A1C
            );
            AICoreBubbler.DisableForSession();
        }
    }
}
