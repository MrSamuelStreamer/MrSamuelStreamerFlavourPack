using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Show skill modifiers/offsets from haunts
/// </summary>
[HarmonyPatch(typeof(SkillUI))]
public static class SkillUI_Patch
{
    [HarmonyPatch("GetSkillDescription")]
    [HarmonyPostfix]
    public static void GetSkillDescription(SkillRecord sk, ref string __result)
    {
        List<HediffComp_Haunt> comps = sk
            .Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>()
            .SelectMany(hediff => hediff.comps)
            .OfType<HediffComp_Haunt>()
            .Where(comp => comp.skillToBoost == sk.def)
            .ToList();

        StringBuilder sb = new(__result);

        foreach (HediffComp_Haunt comp in comps)
        {
            sb.Append("\n  - " + comp.parent.LabelCap + ": " + comp.SkillBoostLevel.ToStringWithSign());
        }

        __result = sb.ToString();
    }
}
