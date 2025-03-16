using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch to allow longer names
/// </summary>
[HarmonyPatch(
    typeof(Dialog_NamePawn),
    MethodType.Constructor,
    [typeof(Pawn), typeof(NameFilter), typeof(NameFilter), typeof(Dictionary<NameFilter, List<string>>), typeof(string), typeof(string), typeof(string), typeof(string)]
)]
public static class Dialog_NamePawn_Patch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S)
            {
                if (instruction.operand is sbyte and (12 or 16))
                {
                    instruction.operand = 4096;
                }
            }

            yield return instruction;
        }
    }
}
