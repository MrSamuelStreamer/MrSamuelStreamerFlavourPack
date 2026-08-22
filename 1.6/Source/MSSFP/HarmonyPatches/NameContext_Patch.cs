using System;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch max name length
/// </summary>
public static class NameContext_Patch
{
    public static Lazy<FieldInfo> textboxWidth = new Lazy<FieldInfo>(() =>
        AccessTools.Field(AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext"), "textboxWidth")
    );
    public static Lazy<FieldInfo> maximumNameLength = new Lazy<FieldInfo>(() =>
        AccessTools.Field(
            AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext"),
            "maximumNameLength"
        )
    );
    public static Lazy<FieldInfo> current = new Lazy<FieldInfo>(() =>
        AccessTools.Field(AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext"), "current")
    );

    private static readonly Regex RichTextTag = new Regex("<[^>]+>", RegexOptions.Compiled);

    public static void Postfix(object __instance)
    {
        textboxWidth.Value?.SetValue(__instance, 400f);
        maximumNameLength.Value?.SetValue(__instance, 4096);

        // Pawn_Patch's name-decoration cache serves rich-text-wrapped strings from
        // Pawn.Name; the rename dialog seeds its textboxes straight from that, so
        // without this the markup gets edited alongside the name and re-saved as
        // part of nameInt, compounding on every rename. Strip it before the row draws.
        if (current.Value?.GetValue(__instance) is string text && text.IndexOf('<') >= 0)
        {
            current.Value.SetValue(__instance, RichTextTag.Replace(text, ""));
        }
    }
}
