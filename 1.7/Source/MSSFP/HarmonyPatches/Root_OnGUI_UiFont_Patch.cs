using HarmonyLib;
using MSSFP.Fonts;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Tells the UI font boot guard that the game is still drawing, so it can clear its crash marker
/// a few frames after a new font is applied. See <see cref="UiFontBootGuard"/>.
/// </summary>
[HarmonyPatch(typeof(Root), nameof(Root.OnGUI))]
public static class Root_OnGUI_UiFont_Patch
{
    [HarmonyPostfix]
    public static void Postfix() => UiFontBootGuard.OnFrameDrawn();
}
