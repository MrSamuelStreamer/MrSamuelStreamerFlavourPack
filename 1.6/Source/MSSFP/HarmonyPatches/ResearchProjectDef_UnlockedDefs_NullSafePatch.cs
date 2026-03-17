using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Fixes a crash in ResearchProjectDef.get_UnlockedDefs where a mod introduces a
/// RecipeDef whose product ThingDef reference is null (failed def resolution). The
/// vanilla LINQ query doesn't guard against null elements, causing a
/// NullReferenceException inside the sort key selector lambda.
///
/// Symptom: "Exception filling window for FluffyResearchTree.MainTabWindow_ResearchTree:
/// System.NullReferenceException" at ResearchProjectDef+<>c.<get_UnlockedDefs>b__83_2
///
/// Fix: Replace the two OrderBy&lt;Def,string&gt; calls inside get_UnlockedDefs with a
/// null-safe wrapper that filters null Defs before sorting. Gated behind a setting.
/// </summary>
[HarmonyPatch(typeof(ResearchProjectDef), "get_UnlockedDefs")]
public static class ResearchProjectDef_UnlockedDefs_NullSafePatch
{
    /// <summary>
    /// Drop-in replacement for Enumerable.OrderBy&lt;Def, string&gt; used by the transpiler.
    /// Filters null Def elements from the source before sorting to prevent NRE when
    /// a broken mod def has an unresolved product ThingDef reference.
    /// </summary>
    public static IOrderedEnumerable<Def> SafeOrderByLabel(
        IEnumerable<Def> source,
        Func<Def, string> keySelector)
    {
        if (MSSFPMod.settings?.NullDefSafetyPatch != true)
            return source.OrderBy(keySelector);

        return source.Where(x => x != null).OrderBy(keySelector);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo safeMethod = AccessTools.Method(
            typeof(ResearchProjectDef_UnlockedDefs_NullSafePatch),
            nameof(SafeOrderByLabel));

        int patchCount = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (IsOrderByDefString(instruction))
            {
                var replacement = new CodeInstruction(OpCodes.Call, safeMethod);
                replacement.labels.AddRange(instruction.labels);
                instruction.labels.Clear();
                yield return replacement;
                patchCount++;
                continue;
            }

            yield return instruction;
        }

        if (patchCount == 0)
            Log.Warning(
                "[MSSFP] ResearchProjectDef_UnlockedDefs_NullSafePatch: could not find " +
                "OrderBy<Def,string> in get_UnlockedDefs — null-safety fix is inactive. " +
                "This may indicate a RimWorld update changed the method.");
    }

    private static bool IsOrderByDefString(CodeInstruction instruction)
    {
        if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            return false;
        if (instruction.operand is not MethodInfo mi)
            return false;
        if (mi.Name != "OrderBy" || !mi.IsGenericMethod)
            return false;

        Type[] args = mi.GetGenericArguments();
        return args.Length == 2 && args[0] == typeof(Def) && args[1] == typeof(string);
    }
}
