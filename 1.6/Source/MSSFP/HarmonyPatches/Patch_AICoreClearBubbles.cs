using HarmonyLib;
using MSSFP.AICore;
using Verse.Profile;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Clears bubble state on game shutdown / map-and-world unload.
///
/// Why this hook: <see cref="MemoryUtility.ClearAllMapsAndWorld"/> is RimWorld's canonical "drop everything"
/// path. Catches Quit-to-Main-Menu, New Game, and Load Save flows. Without this the static dictionary
/// retains <see cref="Verse.Thing"/> refs from the previous map and leaks until app exit.
///
/// Prefix (not Postfix) so we drop our refs BEFORE vanilla starts tearing down maps — avoids any race
/// where <see cref="AICoreBubbler.OnGUI"/> could hit a half-destroyed Thing during the same frame.
/// </summary>
[HarmonyPatch(typeof(MemoryUtility))]
public static class Patch_AICoreClearBubbles
{
    [HarmonyPatch(nameof(MemoryUtility.ClearAllMapsAndWorld))]
    [HarmonyPrefix]
    public static void Prefix()
    {
        AICoreBubbler.Clear();
    }
}
