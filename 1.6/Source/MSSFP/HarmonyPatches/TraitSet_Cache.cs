using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(TraitSet))]
public static class TraitSet_Cache
{
    public static Lazy<FieldInfo> pawn = new(() => AccessTools.Field(typeof(TraitSet), "pawn"));

    [HarmonyPatch("RecacheTraits")]
    [HarmonyPostfix]
    public static void RecacheTraits_Postfix(TraitSet __instance)
    {
        Pawn p = pawn.Value.GetValue(__instance) as Pawn;
        if(p == null) return;

        foreach (Trait trait in __instance.allTraits.Where(t=>t.def.HasModExtension<TraitModDefExtension>()))
        {
            TraitModDefExtension extension = trait.def.GetModExtension<TraitModDefExtension>();
            Pawn_Patch.UpdatePawnName(p, extension);
        }
    }
}
