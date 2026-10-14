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
    [
        typeof(Pawn),
        typeof(NameFilter),
        typeof(NameFilter),
        typeof(Dictionary<NameFilter, List<string>>),
        typeof(string),
        typeof(string),
        typeof(string),
        typeof(string),
    ]
)]
public static class Dialog_NamePawn_Patch
{
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        int replaced = 0;

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_S)
            {
                if (instruction.operand is sbyte and (12 or 16))
                {
                    // Ldc_I4_S's operand is a single signed byte (-128..127), so it
                    // cannot hold 4096. Switch to Ldc_I4, which takes a full int32.
                    instruction.opcode = OpCodes.Ldc_I4;
                    instruction.operand = 4096;
                    replaced++;
                }
            }

            yield return instruction;
        }

        if (replaced != ExpectedReplacements)
        {
            Log.Warning(
                $"[MSSFP] Dialog_NamePawn constructor: replaced {replaced} name-length constants, expected "
                    + $"{ExpectedReplacements}. Name limits may be wrong — a RimWorld update probably changed the constructor."
            );
        }
    }

    // 1.7.4977 has exactly four such constants (12, 16, 12, 16), all name-length arguments (checked against the IL).
    private const int ExpectedReplacements = 4;
}
