using HarmonyLib;
using MSSFP.PawnPortability;
using MSSFP.PawnPortability.Settings;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Draws a "user submitted" icon to the left of a template pawn's map label.
/// IMGUI rich text can't embed images, so the icon is a texture drawn beside
/// the label rather than part of the name string. The Vector2 overload of
/// DrawPawnLabel delegates to this Rect overload, so one patch covers both.
/// </summary>
[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel),
    typeof(Pawn), typeof(Rect), typeof(float), typeof(float),
    typeof(System.Collections.Generic.Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool))]
public static class GenMapUI_TemplateIcon_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, Rect bgRect, float alpha)
    {
        if (!PawnPortabilitySettings.TemplateLabelIconEnabled || Hediff_TemplateOrigin.Of(pawn) == null)
            return;

        float size = bgRect.height;
        Rect iconRect = new(bgRect.x - size - 1f, bgRect.y, size, size);

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(iconRect, TemplateIconTextures.UserSubmitted);
        GUI.color = previous;
    }
}

[StaticConstructorOnStartup]
public static class TemplateIconTextures
{
    // Drop the Flaticon PNG at Common/Textures/UI/MSSFP/UserSubmitted.png.
    // Until then a vanilla icon stands in so the feature is testable.
    public static readonly Texture2D UserSubmitted =
        ContentFinder<Texture2D>.Get("UI/MSSFP/UserSubmitted", false)
        ?? ContentFinder<Texture2D>.Get("UI/Icons/ModRequirements/Installed");
}
