namespace MSSFP.HarmonyPatches;

/// <summary>
/// Show skill modifiers/offsets from haunts - not currently working
/// </summary>
// [HarmonyPatch(typeof(SkillUI))]
public static class SkillUI_Patch
{
    // [HarmonyPatch("GetSkillDescription")]
    // [HarmonyPostfix]
    // public static void GetSkillDescription(SkillRecord sk, ref string __result)
    // {
    //     StringBuilder sb = new StringBuilder(__result);
    //
    //     List<HediffComp_Haunt> comps = sk.Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany(hediff => hediff.comps).OfType<HediffComp_Haunt>().Where(comp=>comp.Pawn != null).ToList();
    //
    //     foreach (HediffComp_Haunt comp in comps)
    //     {
    //         sb.Append("\n  - " + comp.parent.LabelCap + ": " + comp.AptitudeFor(sk.def).ToStringWithSign());
    //     }
    //
    //     __result = sb.ToString();
    // }
}
