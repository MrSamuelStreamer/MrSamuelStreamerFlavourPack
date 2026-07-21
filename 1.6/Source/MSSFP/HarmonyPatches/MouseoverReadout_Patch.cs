using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Comps.Map;
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
#if DEBUG
    [HarmonyDebug]
#endif
    public static IEnumerable<CodeInstruction> MouseoverReadoutOnGUI(
        IEnumerable<CodeInstruction> instructions
    )
    {
        bool found = false;

        // Reflection reference to the injected method
        MethodInfo drawMrStreamerMethod = AccessTools.Method(
            typeof(MouseoverReadout_Patch),
            nameof(DrawMrStreamer)
        );

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
            UnityEngine.Debug.LogError(
                "Initialization point for `num1` not found in MouseoverReadout_Patch.Transpiler"
            );
        }
    }

    public static float DrawMrStreamer()
    {
        float height = 0f;

        if (MSSFPMod.settings.DrawByMrStreamer)
        {
            Widgets.Label(
                new Rect(BotLeft.x, UI.screenHeight - BotLeft.y, 999f, 999f),
                "MSS_MRStreamer".Translate()
            );
            height += 19f;
        }

        height += DrawStructureCredits(height);

        return height;
    }

    /// <summary>
    /// Credits the viewers whose structures were generated onto the current map, stacked directly
    /// above whatever was already drawn (the Mr Streamer label, or nothing if it's disabled).
    ///
    /// Matches vanilla's own convention (see MouseoverReadout.MouseoverReadoutOnGUI): each line gets
    /// its own 19f-tall slot at an incrementing offset, drawn as a separate Widgets.Label call. A
    /// single multi-line label sized by its total height does NOT stack flush against a neighboring
    /// slot drawn this way — it needs one label per slot to line up.
    /// </summary>
    private static float DrawStructureCredits(float baseOffset)
    {
        List<GeneratedStructureRecord> structures = Find.CurrentMap
            ?.GetComponent<GeneratedStructureMapComponent>()
            ?.Structures;
        if (structures.NullOrEmpty())
            return 0f;

        float offset = baseOffset;
        foreach (string line in FormatCreditLines(structures))
        {
            Widgets.Label(
                new Rect(BotLeft.x, UI.screenHeight - BotLeft.y - offset, 999f, 999f),
                line
            );
            offset += 19f;
        }

        return offset - baseOffset;
    }

    /// <summary>One entry per author, defNames deduped and Oxford-comma collapsed within each.</summary>
    private static IEnumerable<string> FormatCreditLines(List<GeneratedStructureRecord> structures)
    {
        return structures
            .GroupBy(s => s.Author.NullOrEmpty() ? null : s.Author)
            .Select(g =>
            {
                string defNames = JoinOxford(g.Select(s => s.DefName).Distinct().ToList());
                return g.Key.NullOrEmpty() ? defNames : $"{defNames} from {g.Key}";
            });
    }

    private static string JoinOxford(List<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[items.Count - 1]}",
    };
}
