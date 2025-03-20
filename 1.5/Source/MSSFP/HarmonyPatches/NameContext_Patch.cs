using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch max name length
/// </summary>
public static class NameContext_Patch
{
    public static Lazy<FieldInfo> textboxWidth = new Lazy<FieldInfo>(() => AccessTools.Field(AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext"), "textboxWidth"));
    public static Lazy<FieldInfo> maximumNameLength = new Lazy<FieldInfo>(() => AccessTools.Field(AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext"), "maximumNameLength"));

    public static void Postfix(object __instance)
    {
        textboxWidth.Value.SetValue(__instance, 400f);
        maximumNameLength.Value.SetValue(__instance, 4096);
    }
}
