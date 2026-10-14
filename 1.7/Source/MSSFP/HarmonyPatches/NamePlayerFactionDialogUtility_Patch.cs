using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch max name length
/// </summary>
[HarmonyPatch(typeof(NamePlayerFactionDialogUtility))]
public static class NamePlayerFactionDialogUtility_Patch
{
    [HarmonyPatch(nameof(NamePlayerFactionDialogUtility.IsValidName))]
    [HarmonyPrefix]
    public static bool IsValidNamePatch(string s, ref bool __result)
    {
        __result =
            s.Length != 0
            && s.Length <= 4096
            && !new Regex("[" + Regex.Escape(GenText.GetInvalidFilenameCharacters()) + "]").IsMatch(
                s
            )
            && !GrammarResolver.ContainsSpecialChars(s);
        return false;
    }
}
