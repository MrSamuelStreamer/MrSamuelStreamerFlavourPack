using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(CompBreakdownable), nameof(CompBreakdownable.CheckForBreakdown))]
public static class CompBreakdownable_CheckForBreakdown_Transpiler
{
    public static float GetCustomMTB(CompBreakdownable comp)
    {
        return comp is not CompBreakdownableConfigurable configurableComp
            ? Mathf.Round(MSSFPMod.settings.BreakdownMTBDays * GenDate.TicksPerDay)
            : configurableComp.BreakdownProperties.mtbBreakdownTicks;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool found = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!found
                && instruction.opcode == OpCodes.Ldc_R4
                && Mathf.Approximately((float) instruction.operand, 13680000f))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CompBreakdownable_CheckForBreakdown_Transpiler), nameof(GetCustomMTB)));
                found = true;
            }
            else
            {
                yield return instruction;
            }
        }

        if (!found)
        {
            Log.Warning("Could not find MTBEventOccurs call in CompBreakdownable.CheckForBreakdown for transpiler.");
        }
    }
}
