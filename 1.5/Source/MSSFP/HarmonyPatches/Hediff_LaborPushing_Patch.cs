using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Hediff_LaborPushing))]
public static class Hediff_LaborPushing_Patch
{
    // TODO: Investigate why this leads to unending labor in the Catharsis2 pack
    // [HarmonyPatch(nameof(Hediff_LaborPushing.PreRemoved))]
    // [HarmonyTranspiler]
    // public static IEnumerable<CodeInstruction> Apply_Transpiler(IEnumerable<CodeInstruction> instructions)
    // {
    //     MethodInfo methodToReplace = AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome_NewTemp");
    //     MethodInfo replacementMethod = AccessTools.Method(typeof(RitualOutcomeEffectWorker_ChildBirth_Patch), "ApplyBirthOutcome_NewTemp");
    //
    //     foreach (CodeInstruction instruction in instructions)
    //     {
    //         if (instruction.opcode == OpCodes.Call && instruction.operand as MethodInfo == methodToReplace)
    //         {
    //             instruction.operand = replacementMethod;
    //         }
    //
    //         yield return instruction;
    //     }
    // }
}
