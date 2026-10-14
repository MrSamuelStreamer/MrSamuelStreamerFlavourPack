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
    public static IEnumerable<CodeInstruction> selectProduct(
        IEnumerable<CodeInstruction> instructions
    )
    {
        List<CodeInstruction> codes = new(instructions);
        int insertIndex = -1;

        // Find the instruction where we check stackLimit
        for (int i = 0; i < codes.Count; i++)
        {
            // Look for the stackLimit field access
            if (
                codes[i].opcode == OpCodes.Ldfld
                && codes[i].operand is FieldInfo { Name: "stackLimit" }
            )
            {
                // Insert our custom check right before the stackLimit check
                insertIndex = i - 1; // Position of the thingDef local variable
                // Only patch the first occurrence
                break;
            }
        }

        if (insertIndex > 0)
        {
            // Find a conditional branch that skips/continues this candidate
            // Accept any short/long conditional branch and capture its target label.
            object continueLabel = null;

            for (int i = insertIndex; i < codes.Count; i++)
            {
                OpCode op = codes[i].opcode;
                bool isCondBranch =
                    op == OpCodes.Brtrue_S || op == OpCodes.Brfalse_S ||
                    op == OpCodes.Brtrue   || op == OpCodes.Brfalse ||
                    op == OpCodes.Bne_Un_S || op == OpCodes.Bne_Un  ||
                    op == OpCodes.Beq_S    || op == OpCodes.Beq;

                if (!isCondBranch) continue;

                // Harmony emits Label as operand for branch destinations
                if (codes[i].operand != null)
                {
                    continueLabel = codes[i].operand;
                    break;
                }
            }

            if (continueLabel == null)
            {
                Log.Error("[MSSFP ResourceGenerator] Could not find branch target in selectProduct method");
                return codes;
            }

            codes.InsertRange(
                insertIndex,
                new[]
                {
                    new CodeInstruction(OpCodes.Dup), // Duplicate thingDef on stack (or the local carrying it)
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(ResourceGenerator_Patches),
                            nameof(ShouldSkipChecks)
                        )
                    ),
                    // If ShouldSkipChecks is true, jump to the same 'continue' label used by original logic
                    new CodeInstruction(OpCodes.Brtrue, continueLabel),
                }
            );
        }
        else
        {
            Log.Error(
                "[MSSFP ResourceGenerator] Could not find insertIndex in selectProduct method"
            );
        }

        return codes;
    }

    // Helper method to determine if we should skip the checks
    public static bool ShouldSkipChecks(object objIn)
    {
        ThingDef thingDef =
            AccessTools.Field(objIn.GetType(), "thingDef").GetValue(objIn) as ThingDef;
        if (thingDef == null)
            return false;
        // Skip checks (i.e., continue the loop) if thingDef is in ExtraBuildables
        bool isInExtraBuildables =
            MSSFPResourceGeneratorMod.settings?.ExtraBuildables?.Contains(thingDef) == true;
        ModLog.Debug($"Checking {thingDef.LabelCap} -> isInExtraBuildables={isInExtraBuildables}");
        return isInExtraBuildables;
    }
}
