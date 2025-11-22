using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MSSFP.ModExtensions;
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

    public static void UpdatePawnName(Pawn pawn, TraitModDefExtension extension)
    {
        if (pawn.Name is NameTriple triple)
        {

            string first = triple.First;
            string last = triple.Last;
            string nick = triple.Nick;


            if (extension.rainbow)
            {
                first = RainbowString(first);
                nick = RainbowString(nick);
                last = RainbowString(last);
            }else if (extension.color.HasValue)
            {
                string colourHex = ColorUtility.ToHtmlStringRGB(extension.color.Value);
                string colourPrefix = "<color=#" + colourHex + ">";
                string colourSuffix = "</color>";

                first = colourPrefix + first + colourSuffix;
                last = colourPrefix + last + colourSuffix;
                nick = colourPrefix + nick + colourSuffix;
            }

            if (extension.bold)
            {
                string boldPrefix = "<b>";
                string boldSuffix = "</b>";

                first = boldPrefix + first + boldSuffix;
                last = boldPrefix + last + boldSuffix;
                nick = boldPrefix + nick + boldSuffix;
            }

            if (extension.italic)
            {
                string italicPrefix = "<i>";
                string italicSuffix = "</i>";

                first = italicPrefix + first + italicSuffix;
                last = italicPrefix + last + italicSuffix;
                nick = italicPrefix + nick + italicSuffix;
            }

            nameInt.Value.SetValue(pawn, new NameTriple(first, nick, last));
        }
    }

    public static string RainbowString(string text)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            sb.Append(ColourPrefix + c + "</color>");
        }

        return sb.ToString();
    }

    [HarmonyPatch(nameof(Pawn.Name), MethodType.Setter)]
    [HarmonyPostfix]
    public static void NameSetter_Postfix(Pawn __instance)
    {
        Name name = nameInt.Value.GetValue(__instance) as Name;
        if (name == null || __instance.story?.traits == null || __instance.story.traits.allTraits.NullOrEmpty())
        {
            return;
        }

        foreach (Trait trait in __instance.story.traits.allTraits.Where(t=>t.def.HasModExtension<TraitModDefExtension>()))
        {
            TraitModDefExtension extension = trait.def.GetModExtension<TraitModDefExtension>();
            UpdatePawnName(__instance, extension);
        }
    }
}
