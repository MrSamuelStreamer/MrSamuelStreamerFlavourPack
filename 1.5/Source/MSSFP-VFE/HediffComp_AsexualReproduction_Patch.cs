using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AnimalBehaviours;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace MSSFP.VFE;

[HarmonyPatch(typeof(HediffComp_AsexualReproduction))]
public static class HediffComp_AsexualReproduction_Patch
{
    public static Lazy<FieldInfo> asexualFissionCounter = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(HediffComp_AsexualReproduction), nameof(HediffComp_AsexualReproduction.asexualFissionCounter)));
    public static Lazy<MethodInfo> PlaySquelchInfo = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(HediffComp_AsexualReproduction_Patch), nameof(HediffComp_AsexualReproduction_Patch.PlaySquelch)));

    [HarmonyPatch(nameof(HediffComp_AsexualReproduction.CompPostTick))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> CompPostTick(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> insts = new List<CodeInstruction>(instructions);
        for (int i = 2; i < insts.Count; i++)
        {
            if (insts[i - 2].opcode == OpCodes.Ldarg_0 && insts[i - 1].opcode == OpCodes.Ldc_I4_0 && insts[i].opcode == OpCodes.Stfld &&
                insts[i].operand == asexualFissionCounter.Value)
            {
                insts.InsertRange(i+1, new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, PlaySquelchInfo.Value)
                });
            }
        }

        return insts;
    }

    public static void PlaySquelch(HediffComp_AsexualReproduction __instance)
    {
        MSSFPCFEDefOf.MSSFP_Squelch.PlayOneShot(SoundInfo.InMap(__instance.Pawn));
    }
}
