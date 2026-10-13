using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="CompArt.GenerateImageDescription"/>: if the parent thing carries a
/// <see cref="CompTrueAICoreArt"/> with a non-empty <c>flavouredDescription</c>, swap the
/// vanilla taleRef-generated description for the personality line.
///
/// WHY POSTFIX (not Prefix-with-skip): cheaper to let vanilla compute its TaggedString, throw
/// it away, and re-assign — vanilla path also primes <c>taleRef</c> on first call (see
/// <see cref="CompArt.GenerateImageDescription"/> log + auto-init). Skipping it via prefix
/// would leave taleRef null and then the next caller (e.g. another mod prefixing the same
/// method) trips the auto-init log spam path.
///
/// Side effect: vanilla's "art author" line is appended downstream by
/// <see cref="CompArt.GetDescriptionPart"/> — we override the IMAGE description only. Author
/// + title are still vanilla-shape (with our SetAuthor reflection-set + worker title hook
/// in CompTrueAICore.TryCompleteArt). Intentional: keeps art usable for vanilla art-quality
/// readouts.
/// </summary>
[HarmonyPatch(typeof(CompArt))]
public static class Patch_CompArt_FlavouredDescription
{
    [HarmonyPatch(nameof(CompArt.GenerateImageDescription))]
    [HarmonyPostfix]
    public static void Postfix(CompArt __instance, ref TaggedString __result)
    {
        if (__instance?.parent == null) return;
        CompTrueAICoreArt sidecar = __instance.parent.TryGetComp<CompTrueAICoreArt>();
        if (sidecar == null) return;
        if (string.IsNullOrEmpty(sidecar.flavouredDescription)) return;
        __result = new TaggedString(sidecar.flavouredDescription);
    }
}
