using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Swaps body (and head, when art exists) graphics for pawns with MSSFP_SkinnedAlive.
/// PawnRenderNode_Body and PawnRenderNode_Head override GraphicFor, so the postfix
/// has to land on those overrides. A render-node added by the hediff can only draw
/// on top of the vanilla body, which leaves tinted skin around soft edges.
/// The peeled art is pre-coloured, so it uses Cutout and white instead of the
/// pawn's skin shader. Priority.High runs before the holo-shader postfix, which
/// can still retint the replacement graphic.
/// </summary>
internal static class PeeledPawnGraphics
{
    private const string BodyTexPrefix = "Pawns/Peeled_";
    private const string HeadTexPrefix = "Pawns/PeeledHead_";

    // ContentFinder misses are cached here. GraphicDatabase already caches hits.
    private static readonly HashSet<string> MissingTexPaths = new();

    public static bool ShouldReplace(Pawn pawn)
    {
        return pawn?.health?.hediffSet != null
            && pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSSFP_SkinnedAlive)
            && !pawn.Drawer.renderer.StatueColor.HasValue
            && pawn.Drawer.renderer.CurRotDrawMode != RotDrawMode.Dessicated;
    }

    public static string BodyTexPathFor(Pawn pawn) =>
        pawn.story?.bodyType == null ? null : BodyTexPrefix + pawn.story.bodyType.defName;

    // No PeeledHead_* art is shipped. A miss leaves the vanilla head, tinted by
    // the hediff's skinColorOverride. Matching art is picked up with no code change.
    public static string HeadTexPathFor(Pawn pawn) =>
        pawn.story?.headType == null ? null : HeadTexPrefix + pawn.story.headType.defName;

    public static Graphic TryGetReplacement(string path)
    {
        if (path == null || MissingTexPaths.Contains(path))
            return null;

        if (ContentFinder<Texture2D>.Get(path + "_south", reportFailure: false) == null)
        {
            MissingTexPaths.Add(path);
            return null;
        }

        return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, Color.white);
    }
}

[HarmonyPatch(typeof(PawnRenderNode_Body), nameof(PawnRenderNode_Body.GraphicFor))]
[HarmonyPriority(Priority.High)]
public static class Harmony_PeeledPawn_Body
{
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (!PeeledPawnGraphics.ShouldReplace(pawn))
            return;

        // Child and Baby have no peeled art, so the tinted vanilla body remains.
        Graphic replacement = PeeledPawnGraphics.TryGetReplacement(PeeledPawnGraphics.BodyTexPathFor(pawn));
        if (replacement != null)
            __result = replacement;
    }
}

[HarmonyPatch(typeof(PawnRenderNode_Head), nameof(PawnRenderNode_Head.GraphicFor))]
[HarmonyPriority(Priority.High)]
public static class Harmony_PeeledPawn_Head
{
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (!PeeledPawnGraphics.ShouldReplace(pawn))
            return;

        Graphic replacement = PeeledPawnGraphics.TryGetReplacement(PeeledPawnGraphics.HeadTexPathFor(pawn));
        if (replacement != null)
            __result = replacement;
    }
}

/// <summary>
/// Body tattoos draw on top of the body node. Suppress them on a peeled pawn so
/// they do not sit on the raw-flesh texture.
/// </summary>
[HarmonyPatch(typeof(PawnRenderNode_Tattoo_Body), nameof(PawnRenderNode_Tattoo_Body.GraphicFor))]
[HarmonyPriority(Priority.High)]
public static class Harmony_PeeledPawn_SuppressTattoo
{
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (PeeledPawnGraphics.ShouldReplace(pawn))
            __result = null;
    }
}
