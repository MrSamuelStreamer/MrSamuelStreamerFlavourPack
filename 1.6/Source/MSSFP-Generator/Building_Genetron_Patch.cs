using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using VanillaQuestsExpandedTheGenerator;

namespace MSSFP.GeneratorMod;

[HarmonyPatch]
public static class GenetronConstants_Transpiler
{
    private static IEnumerable<CodeInstruction> ReplaceIntConstant(
        IEnumerable<CodeInstruction> instructions,
        int originalValue,
        int newValue
    )
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (
                instruction.opcode == OpCodes.Ldc_I4
                && instruction.operand is int value
                && value == originalValue
            )
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4, newValue);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    private static MethodBase FindIteratorMoveNext(Type type, string methodName)
    {
        try
        {
            var nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var nestedType in nestedTypes)
            {
                if (nestedType.Name.Contains($"<{methodName}>"))
                {
                    return AccessTools.Method(nestedType, "MoveNext");
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    [HarmonyPatch]
    public static class Basic_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_Basic), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 180000, 45000);
        }
    }

    [HarmonyPatch]
    public static class WoodPowered_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_WoodPowered), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 1800000, 450000);
        }
    }

    [HarmonyPatch]
    public static class ThermalVent_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_ThermalVent), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 600000, 150000);
        }
    }

    [HarmonyPatch]
    public static class Geothermal_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_Geothermal), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 900000, 225000);
        }
    }

    [HarmonyPatch]
    public static class WoodFueled_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_WoodFueled), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 1500, 375);
        }
    }

    [HarmonyPatch]
    public static class WoodFired_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_WoodFired), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 700, 175);
        }
    }

    [HarmonyPatch]
    public static class ChemfuelCharged_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_ChemfuelCharged), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 1000, 250);
        }
    }

    [HarmonyPatch]
    public static class Nuclear_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_Nuclear), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 500, 125);
        }
    }

    [HarmonyPatch]
    public static class ChemfuelBoosted_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return FindIteratorMoveNext(typeof(Building_Genetron_ChemfuelBoosted), "GetGizmos");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions
        )
        {
            return ReplaceIntConstant(instructions, 3, 1);
        }
    }
}
