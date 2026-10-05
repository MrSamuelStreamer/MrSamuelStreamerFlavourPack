using System.Collections.Generic;
using HarmonyLib;
using MSSFP.LabelIcons;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Draws a row of status icons to the left of a humanlike pawn's map label
/// (template marker, drafted, bleeding, ...). IMGUI rich text can't embed
/// images, so icons are textures drawn beside the label rather than part of
/// the name string. The Vector2 overload of DrawPawnLabel delegates to this
/// Rect overload, so one patch covers both.
/// </summary>
[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel),
    typeof(Pawn), typeof(Rect), typeof(float), typeof(float),
    typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool))]
public static class GenMapUI_LabelIcons_Patch
{
    private const float PulseFrequency = 1.5f;
    private const float PulseAmplitude = 0.6f;

    // Reused across calls; DrawPawnLabel only runs on the main thread.
    private static readonly List<LabelIcon> Icons = new();

    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, Rect bgRect, float alpha)
    {
        if (pawn?.RaceProps?.Humanlike != true) return;

        LabelIcons.LabelIcons.Collect(pawn, Icons);
        if (Icons.Count == 0) return;

        float size = bgRect.height;
        float x = bgRect.x - 1f;
        Color previous = GUI.color;

        foreach (LabelIcon icon in Icons)
        {
            x -= size;
            float iconAlpha = icon.Pulse ? alpha * Pulser.PulseBrightness(PulseFrequency, PulseAmplitude) : alpha;
            GUI.color = new Color(icon.Tint.r, icon.Tint.g, icon.Tint.b, iconAlpha);
            GUI.DrawTexture(new Rect(x, bgRect.y, size, size), icon.Texture);
            x -= 1f;
        }

        GUI.color = previous;
    }
}
