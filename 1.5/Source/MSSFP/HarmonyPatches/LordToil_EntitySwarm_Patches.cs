using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(LordToil_EntitySwarm))]
public static class LordToil_EntitySwarm_Patches
{
    [HarmonyPatch(nameof(LordToil_EntitySwarm.LordToilTick))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> codes = new(instructions);
        int targetIndex = -1;

        // Find the index of the instruction to patch.
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("Verse.Pawn::pather"))
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex != -1)
        {
            Label labelToReturn = il.DefineLabel();
            Label labelAfterNullCheck = il.DefineLabel();

            List<CodeInstruction> newInstructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldloc_1), // Load ownedPawn
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "pather")), // Load pather field
                new(OpCodes.Brtrue_S, labelAfterNullCheck), // If not null, continue

                // Instructions to call base.LordToilTick() and return
                new(OpCodes.Ldarg_0), // Load 'this'
                new(OpCodes.Call, AccessTools.Method(typeof(LordToil), "LordToilTick")), // Call base.LordToilTick()
                new(OpCodes.Br, labelToReturn), // Return from method

                // Label for continuing if pather is not null
                new(OpCodes.Nop) { labels = new List<Label> { labelAfterNullCheck } }, // Define label position
                codes[targetIndex], // Original ldfld
                codes[targetIndex + 1] // Original callvirt
            };

            // Attach return label to the last original instruction
            codes[codes.Count - 1].labels.Add(labelToReturn);

            // Remove the original instructions and insert the new ones
            codes.RemoveRange(targetIndex, 2);
            codes.InsertRange(targetIndex, newInstructions);
        }

        return codes.AsEnumerable();
    }
}
