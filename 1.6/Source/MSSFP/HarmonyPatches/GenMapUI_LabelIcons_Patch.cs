using System;
using System.Collections.Generic;
using HarmonyLib;
using MSSFP.LabelIcons;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Draws a row of status icons to the left of a humanlike pawn's centred label
/// (map labels, colonist bar). IMGUI rich text can't embed images, so icons are
/// textures drawn beside the label rather than part of the name string. The
/// Vector2 overload of DrawPawnLabel delegates to this Rect overload, so one
/// patch covers both.
/// </summary>
[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel),
    typeof(Pawn), typeof(Rect), typeof(float), typeof(float),
    typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool))]
public static class GenMapUI_LabelIcons_Patch
{
    private const float PulseFrequency = 1.5f;
    private const float PulseAmplitude = 0.6f;

    // The colonist bar packs portraits tightly; more icons run into the
    // neighbouring portrait. Icons are in urgency order, so the cap keeps
    // the most urgent ones.
    private const int ColonistBarIconCap = 3;

    // Set while ColonistBarColonistDrawer.DrawColonist runs. Main thread only.
    internal static bool InColonistBar;

    // Reused across calls; DrawPawnLabel only runs on the main thread.
    private static readonly List<LabelIcon> Icons = new();

    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, Rect bgRect, float alpha, bool alignCenter)
    {
        // Left-aligned labels are caravan list rows (world tabs), where the
        // space left of the label belongs to the previous column.
        if (!alignCenter || pawn?.RaceProps?.Humanlike != true) return;

        LabelIcons.LabelIcons.Collect(pawn, Icons);
        if (Icons.Count == 0) return;

        int count = InColonistBar ? Math.Min(Icons.Count, ColonistBarIconCap) : Icons.Count;
        float size = bgRect.height;
        float x = bgRect.x - 1f;
        Color previous = GUI.color;

        for (int i = 0; i < count; i++)
        {
            LabelIcon icon = Icons[i];
            x -= size;
            float iconAlpha = icon.Pulse ? alpha * Pulser.PulseBrightness(PulseFrequency, PulseAmplitude) : alpha;
            GUI.color = new Color(icon.Tint.r, icon.Tint.g, icon.Tint.b, iconAlpha);
            GUI.DrawTexture(new Rect(x, bgRect.y, size, size), icon.Texture);
            x -= 1f;
        }

        GUI.color = previous;
    }
}

/// <summary>Flags colonist-bar label draws so the icon row can be capped there.</summary>
[HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
public static class ColonistBarColonistDrawer_LabelIcons_Patch
{
    [HarmonyPrefix]
    public static void Prefix() => GenMapUI_LabelIcons_Patch.InColonistBar = true;

    // Finalizer, not postfix: resets the flag even if drawing throws, so map
    // labels never inherit the cap.
    [HarmonyFinalizer]
    public static void Finalizer() => GenMapUI_LabelIcons_Patch.InColonistBar = false;
}
