using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using MSSFP.ModExtensions;
using MSSFP.PawnPortability;
using MSSFP.PawnPortability.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
public static class Pawn_Patch
{
    public static int Index = 0;
    public static List<string> RainbowColours = ["red", "orange", "yellow", "lime", "cyan", "blue", "purple"];

    public static string NextColour => RainbowColours[Index++ % RainbowColours.Count];

    public static string ColourPrefix => $"<color={NextColour}>";

    public static Lazy<FieldInfo> nameInt = new(() => AccessTools.Field(typeof(Pawn), "nameInt"));

    public static readonly ConditionalWeakTable<Pawn, Name> NameCache = new();

    /// <summary>The undecorated name, bypassing the decorating getter.</summary>
    public static Name RawName(Pawn pawn) => nameInt.Value.GetValue(pawn) as Name;

    /// <summary>Kept for TraitSet_Cache; decoration is now rebuilt as a whole.</summary>
    public static void UpdatePawnName(Pawn pawn, TraitModDefExtension extension) => Redecorate(pawn);

    /// <summary>
    /// Rebuilds the decorated name from the raw name. The first trait extension
    /// styles the name; the template colour wraps the result, so a trait colour
    /// (innermost tag) still wins and the template colour acts as a fallback.
    /// </summary>
    public static void Redecorate(Pawn pawn)
    {
        if (pawn == null) return;

        // Always drop the old decoration first so a rename or settings change
        // can't leave the getter returning a stale decorated name.
        NameCache.Remove(pawn);

        if (RawName(pawn) is not NameTriple triple) return;

        string first = triple.First;
        string nick = triple.Nick;
        string last = triple.Last;
        bool decorated = false;

        TraitModDefExtension extension = FirstNameExtension(pawn);
        if (extension != null)
        {
            ApplyTraitDecoration(extension, ref first, ref nick, ref last);
            decorated = true;
        }

        string templateHex = TemplateColourHex(pawn);
        if (templateHex != null)
        {
            first = Wrap(first, $"<color=#{templateHex}>", "</color>");
            nick = Wrap(nick, $"<color=#{templateHex}>", "</color>");
            last = Wrap(last, $"<color=#{templateHex}>", "</color>");
            decorated = true;
        }

        if (decorated)
            NameCache.Add(pawn, new NameTriple(first, nick, last));
    }

    /// <summary>Rebuilds decorations for every pawn in the game (load, settings change).</summary>
    public static void RedecorateAll()
    {
        if (Current.Game == null) return;

        foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
            Redecorate(pawn);
    }

    private static TraitModDefExtension FirstNameExtension(Pawn pawn) =>
        pawn.story?.traits?.allTraits?
            .Select(t => t.def.GetModExtension<TraitModDefExtension>())
            .FirstOrDefault(e => e != null);

    // Hostile templates keep vanilla's red label unless the player opts in, so a
    // green name never hides that a raider is an enemy.
    private static string TemplateColourHex(Pawn pawn)
    {
        if (Hediff_TemplateOrigin.Of(pawn) == null) return null;

        bool hostile = pawn.Faction != null && Faction.OfPlayerSilentFail != null
                       && pawn.Faction.HostileTo(Faction.OfPlayerSilentFail);
        if (hostile && !PawnPortabilitySettings.ColourHostileTemplateNames) return null;

        return PawnPortabilitySettings.TemplateNameColourHex;
    }

    private static void ApplyTraitDecoration(
        TraitModDefExtension extension, ref string first, ref string nick, ref string last)
    {
        if (extension.rainbow)
        {
            first = RainbowString(first);
            nick = RainbowString(nick);
            last = RainbowString(last);
        }
        else if (extension.color.HasValue)
        {
            string colourPrefix = "<color=#" + ColorUtility.ToHtmlStringRGB(extension.color.Value) + ">";
            first = Wrap(first, colourPrefix, "</color>");
            nick = Wrap(nick, colourPrefix, "</color>");
            last = Wrap(last, colourPrefix, "</color>");
        }

        if (extension.bold)
        {
            first = Wrap(first, "<b>", "</b>");
            nick = Wrap(nick, "<b>", "</b>");
            last = Wrap(last, "<b>", "</b>");
        }

        if (extension.italic)
        {
            first = Wrap(first, "<i>", "</i>");
            nick = Wrap(nick, "<i>", "</i>");
            last = Wrap(last, "<i>", "</i>");
        }
    }

    private static string Wrap(string text, string prefix, string suffix) =>
        string.IsNullOrEmpty(text) ? text : prefix + text + suffix;

    public static string RainbowString(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            sb.Append(ColourPrefix + c + "</color>");
        }

        return sb.ToString();
    }


    [HarmonyPatch(nameof(Pawn.Name), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool NameGetter_Prefix(Pawn __instance, ref Name __result)
    {
        if (NameCache.TryGetValue(__instance, out Name name))
        {
            __result = name;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Pawn.Name), MethodType.Setter)]
    [HarmonyPostfix]
    public static void NameSetter_Postfix(Pawn __instance) => Redecorate(__instance);

    // Hostility drives template colour, so recruiting or a defection must rebuild it.
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    [HarmonyPostfix]
    public static void SetFaction_Postfix(Pawn __instance) => Redecorate(__instance);
}
