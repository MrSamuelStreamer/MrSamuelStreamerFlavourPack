using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch to handle LordToilTick getting a null pather. Not sure whos bug it is
/// </summary>
[HarmonyPatch(typeof(LordToil_EntitySwarm))]
public static class LordToil_EntitySwarm_Patches
{
    private static readonly FieldInfo PatherField = AccessTools.Field(typeof(Pawn), nameof(Pawn.pather));

    [HarmonyPatch(nameof(LordToil_EntitySwarm.LordToilTick))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator il
    )
    {
        List<CodeInstruction> codes = new(instructions);
        int targetIndex = -1;

        // Find the index of the instruction to patch.
        for (int i = 0; i < codes.Count; i++)
        {
            // Compare the real field: FieldInfo.ToString() is "Type name", never "Pawn::pather",
            // so the old string match could not hit and the guard silently never applied.
            if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand as FieldInfo == PatherField)
            {
                targetIndex = i;
                break;
            }
        }

        // The vanilla code loads the pawn (ldloc.1) right before `ldfld pather`. Insert the null
        // check BEFORE that load so the evaluation stack is empty on both paths.
        // Harmony may present short forms (ldloc.1) as ldloc + local, so match on the local index.
        bool pawnLoadBeforeAnchor =
            targetIndex >= 1
            && codes[targetIndex - 1].IsLdloc()
            && codes[targetIndex - 1].LocalIndex() == 1;

        if (!pawnLoadBeforeAnchor)
        {
            string before = targetIndex >= 1 ? codes[targetIndex - 1].ToString() : "(none)";
            Log.Error(
                "[MSSFP] LordToil_EntitySwarm.LordToilTick: expected `ldloc.1; ldfld Pawn::pather` was not found "
                    + $"(instruction before the anchor: {before}) — the null-pather guard is inactive. "
                    + "A RimWorld update probably changed the method."
            );
            return codes;
        }

        Label labelAfterNullCheck = il.DefineLabel();
        CodeInstruction pawnLoad = codes[targetIndex - 1];

        List<CodeInstruction> guard = new List<CodeInstruction>
        {
            // Keep any jump targets that pointed at the original pawn load.
            new(OpCodes.Ldloc_1) { labels = new List<Label>(pawnLoad.labels) },
            new(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn), "pather")),
            new(OpCodes.Brtrue_S, labelAfterNullCheck),
            // pather is null: run base.LordToilTick() and return.
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, AccessTools.Method(typeof(LordToil), "LordToilTick")),
            new(OpCodes.Ret),
        };

        pawnLoad.labels.Clear();
        pawnLoad.labels.Add(labelAfterNullCheck);
        codes.InsertRange(targetIndex - 1, guard);

        return codes.AsEnumerable();
    }
}
