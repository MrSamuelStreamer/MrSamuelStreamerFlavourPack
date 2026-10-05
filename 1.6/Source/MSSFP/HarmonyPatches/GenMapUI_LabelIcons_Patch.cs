using System;
using System.Collections.Generic;
using HarmonyLib;
using MSSFP.LabelIcons;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Draws status icons inside a humanlike pawn's centred map label. IMGUI rich
/// text can't embed images, so icons are textures.
///
/// When a pawn has icons, this prefix draws the whole label itself so icons
/// and name share one background and one continuous health fill, centred as
/// a group where the vanilla label would sit. It mirrors vanilla's Rect
/// overload and reuses vanilla's (private) label helpers, so patches to
/// those still apply. Pawns without icons, and left-aligned labels, run
/// vanilla untouched. The Vector2 overload delegates here, so one patch
/// covers both.
/// </summary>
[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel),
    typeof(Pawn), typeof(Rect), typeof(float), typeof(float),
    typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool))]
public static class GenMapUI_LabelIcons_Patch
{
    private const float PulseFrequency = 1.5f;
    private const float PulseAmplitude = 0.6f;
    private const float IconGap = 1f;

    // Set while ColonistBarColonistDrawer.DrawColonist runs. Main thread only.
    // The colonist bar packs portraits too tightly for an icon row, so its
    // labels stay vanilla.
    internal static bool InColonistBar;

    // Reused across calls; DrawPawnLabel only runs on the main thread.
    private static readonly List<LabelIcon> Icons = new();

    private static readonly Func<Pawn, float, Dictionary<string, string>, GameFont, string> GetPawnLabel =
        AccessTools.MethodDelegate<Func<Pawn, float, Dictionary<string, string>, GameFont, string>>(
            AccessTools.Method(typeof(GenMapUI), "GetPawnLabel"));

    private static readonly Func<Pawn, float, Dictionary<string, string>, GameFont, float> GetPawnLabelNameWidth =
        AccessTools.MethodDelegate<Func<Pawn, float, Dictionary<string, string>, GameFont, float>>(
            AccessTools.Method(typeof(GenMapUI), "GetPawnLabelNameWidth"));

    [HarmonyPrefix]
    public static bool Prefix(Pawn pawn, Rect bgRect, float alpha, float truncateToWidth,
        Dictionary<string, string> truncatedLabelsCache, GameFont font, bool alwaysDrawBg, bool alignCenter)
    {
        // Left-aligned labels are caravan list rows (world tabs), where the
        // space left of the label belongs to the previous column.
        if (!alignCenter || InColonistBar || pawn?.RaceProps?.Humanlike != true) return true;

        LabelIcons.LabelIcons.Collect(pawn, Icons);
        if (Icons.Count == 0) return true;

        DrawLabelWithIcons(pawn, bgRect, alpha, truncateToWidth, truncatedLabelsCache, font, alwaysDrawBg, Icons.Count);
        return false;
    }

    private static void DrawLabelWithIcons(Pawn pawn, Rect bgRect, float alpha, float truncateToWidth,
        Dictionary<string, string> truncatedLabelsCache, GameFont font, bool alwaysDrawBg, int iconCount)
    {
        GUI.color = new Color(1f, 1f, 1f, alpha);
        Text.Font = font;
        string label = GetPawnLabel(pawn, truncateToWidth, truncatedLabelsCache, font);
        float labelWidth = GetPawnLabelNameWidth(pawn, truncateToWidth, truncatedLabelsCache, font);

        // Grow the vanilla rect by the icon strip, half to each side, so the
        // group stays centred where the vanilla label would be.
        float iconSize = bgRect.height;
        float iconsWidth = iconCount * (iconSize + IconGap);
        Rect groupRect = new(bgRect.x - iconsWidth / 2f, bgRect.y, bgRect.width + iconsWidth, bgRect.height);
        float nameCentreX = bgRect.center.x + iconsWidth / 2f;

        float health = pawn.health.summaryHealth.SummaryHealthPercent;
        if (alwaysDrawBg || health < 0.999f)
            GUI.DrawTexture(groupRect, TexUI.GrayTextBG);
        if (health < 0.999f)
            Widgets.FillableBar(groupRect.ContractedBy(1f), health, GenMapUI.OverlayHealthTex, BaseContent.ClearTex, doBorder: false);

        DrawIcons(groupRect.x + iconsWidth, bgRect.y, iconSize, alpha, iconCount);

        Color nameColour = PawnNameColorUtility.PawnNameColorOf(pawn);
        nameColour.a = alpha;
        GUI.color = nameColour;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(new Rect(nameCentreX - labelWidth / 2f, bgRect.y - 2f, labelWidth, 100f), label);

        if (pawn.Drafted)
            Widgets.DrawLineHorizontal(nameCentreX - labelWidth / 2f, bgRect.y + 11f + (!Text.TinyFontSupported ? 3 : 0), labelWidth);

        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    // Right to left from the name, so the most urgent icon sits nearest it.
    private static void DrawIcons(float rightEdge, float y, float size, float alpha, int count)
    {
        float x = rightEdge;
        for (int i = 0; i < count; i++)
        {
            LabelIcon icon = Icons[i];
            x -= size + IconGap;
            float iconAlpha = icon.Pulse ? alpha * Pulser.PulseBrightness(PulseFrequency, PulseAmplitude) : alpha;
            GUI.color = new Color(icon.Tint.r, icon.Tint.g, icon.Tint.b, iconAlpha);
            GUI.DrawTexture(new Rect(x, y, size, size), icon.Texture);
        }
    }
}

/// <summary>Flags colonist-bar label draws so they skip the icon row.</summary>
[HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
public static class ColonistBarColonistDrawer_LabelIcons_Patch
{
    [HarmonyPrefix]
    public static void Prefix() => GenMapUI_LabelIcons_Patch.InColonistBar = true;

    // Finalizer, not postfix: resets the flag even if drawing throws, so map
    // labels never lose their icons.
    [HarmonyFinalizer]
    public static void Finalizer() => GenMapUI_LabelIcons_Patch.InColonistBar = false;
}
