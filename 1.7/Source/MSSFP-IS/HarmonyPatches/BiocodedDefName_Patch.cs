using System;
using HarmonyLib;
using Verse;

namespace MSSFP.IS.HarmonyPatches;

/// <summary>
/// Remaps every old per-source biocoded defName (<c>MSSFP_Biocoded_&lt;source&gt;</c>) to the
/// single generic <c>MSS_Biocoded_Thing</c> introduced when the per-implant variant generator
/// was removed. Without this, a save holding one of the old defNames fails to resolve the
/// ThingDef at load and silently drops the item.
///
/// <see cref="Verse.BackCompatibility.BackCompatibleDefName"/> is exactly the hook the game
/// itself uses to resolve renamed/removed defNames during load, and running as a Postfix
/// means it applies regardless of whether the vanilla method took its early-out path.
/// </summary>
[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.BackCompatibleDefName))]
public static class BiocodedDefName_Patch
{
    private const string OldDefNamePrefix = "MSSFP_Biocoded_";
    private const string NewDefName = "MSS_Biocoded_Thing";

    [HarmonyPostfix]
    public static void Postfix(Type defType, string defName, ref string __result)
    {
        if (defType == typeof(ThingDef) && defName != null && defName.StartsWith(OldDefNamePrefix))
            __result = NewDefName;
    }
}
