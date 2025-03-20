using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Draw the Mr Streamer overlay
/// </summary>
[HarmonyPatch(typeof(MouseoverReadout))]
public static class MouseoverReadout_Patch
{
    private static readonly Vector2 BotLeft = new Vector2(15f, 65f);

    [HarmonyPatch(nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    public static IEnumerable<CodeInstruction> MouseoverReadoutOnGUI(IEnumerable<CodeInstruction> instructions)
    {
        bool found = false;

        // Reflection reference to the injected method
        MethodInfo drawMrStreamerMethod = AccessTools.Method(typeof(MouseoverReadout_Patch), nameof(DrawMrStreamer));

        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction curInstruction = codes[i];

            // Look for the IL code initializing `num1` with 0.0 (ldc.r4 0.0 and stloc.1)
            if (
                !found
                && curInstruction.opcode == OpCodes.Ldc_R4
                && curInstruction.operand is float f
                && f == 0.0f
                && i + 1 < codes.Count
                && codes[i + 1].opcode == OpCodes.Stloc_1
            )
            {
                found = true;

                // Replace with: `float num1 = DrawMrStreamer();`
                CodeInstruction inst = new CodeInstruction(OpCodes.Call, drawMrStreamerMethod);
                inst.labels = curInstruction.labels;
                yield return inst; // Call DrawMrStreamer (returns float)
                // yield return new CodeInstruction(OpCodes.Stloc_1);                   // Store the result into num1
                //
                // // Skip the next instruction (`stloc.1`) since we already replaced it
                // i++;
            }
            else
            {
                yield return curInstruction; // Emit unmodified instructions
            }
        }

        if (!found)
        {
            UnityEngine.Debug.LogError("Initialization point for `num1` not found in MouseoverReadout_Patch.Transpiler");
        }
    }

    public static float DrawMrStreamer()
    {
        if (!MSSFPMod.settings.DrawByMrStreamer)
            return 0;

        Widgets.Label(new Rect(BotLeft.x, UI.screenHeight - BotLeft.y, 999f, 999f), "MSS_MRStreamer".Translate());

        return 19f;
    }
}
