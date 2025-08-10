using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

[HarmonyPatch(typeof(ResourceGenerator.ResourceGenerator))]
public static class ResourceGenerator_Patches
{
    [HarmonyPatch("selectProduct")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> selectProduct(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new(instructions);
        int insertIndex = -1;

        // Find the instruction where we check stackLimit
        for (int i = 0; i < codes.Count; i++)
        {
            // Look for the stackLimit field access
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo { Name: "stackLimit" })
            {
                // Insert our custom check right before the stackLimit check
                insertIndex = i - 1; // Position of the thingDef local variable
                // Only patch the first occurrence
                break;
            }
        }

        if (insertIndex > 0)
        {
            // Find the branch instruction that jumps to 'continue' if condition is true
            // It should be a few instructions after the stackLimit check
            int branchIndex = -1;

            for (int i = insertIndex; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode != OpCodes.Brfalse_S || codes[i + 1].opcode != OpCodes.Nop)
                    continue;

                branchIndex = i;
                break;
            }

            if (branchIndex == -1)
            {
                Log.Error("[MSSFP ResourceGenerator] Could not find branch instruction in selectProduct method");
                return codes;
            }

            // Get the continue label from the branch instruction
            object continueLabel = codes[branchIndex].operand;

            codes.InsertRange(
                insertIndex,
                [
                    new CodeInstruction(OpCodes.Dup), // Duplicate thingDef on stack
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ResourceGenerator_Patches), nameof(ShouldSkipChecks))),
                    new CodeInstruction(OpCodes.Brtrue, continueLabel), // If ShouldSkipChecks returns true, jump to continue
                ]
            );
        }
        else
        {
            Log.Error("[MSSFP ResourceGenerator] Could not find insertIndex in selectProduct method");
        }

        return codes;
    }

    // Helper method to determine if we should skip the checks
    public static bool ShouldSkipChecks(object objIn)
    {
        ThingDef thingDef = AccessTools.Field(objIn.GetType(), "thingDef").GetValue(objIn) as ThingDef;
        if (thingDef == null)
            return false;
        // Skip checks (i.e., continue the loop) if thingDef is in ExtraBuildables
        bool isInExtraBuildables = MSSFPResourceGeneratorMod.settings?.ExtraBuildables?.Contains(thingDef) == true;
        ModLog.Debug($"Checking {thingDef.LabelCap} -> isInExtraBuildables={isInExtraBuildables}");
        return isInExtraBuildables;
    }
}
