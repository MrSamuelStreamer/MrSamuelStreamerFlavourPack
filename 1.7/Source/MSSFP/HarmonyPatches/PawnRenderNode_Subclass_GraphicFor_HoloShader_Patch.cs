using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Multi-target postfix on every <c>PawnRenderNode</c> subclass that overrides
/// <see cref="PawnRenderNode.GraphicFor"/> and bypasses the base flow.
///
/// Why one patch with <see cref="HarmonyPatch.TargetMethods"/> instead of N classes:
///   1. Identical postfix body — keep DRY.
///   2. Each subclass override directly calls into <c>HeadTypeDef/BeardDef/HairDef.GetGraphic</c>
///      or <c>GraphicDatabase.Get</c> with the pawn's own <see cref="PawnRenderNode.ShaderFor"/>
///      + <see cref="PawnRenderNode.ColorFor"/>. None route through the base method, so a
///      base-only patch leaves body/head/hair/beard/fur/tattoos un-tinted.
///   3. The base <see cref="PawnRenderNode_GraphicFor_HoloShader_Patch"/> stays as a
///      belt-and-braces catch-all for nodes that don't override.
///
/// Subclasses targeted (from ilspy of Assembly-CSharp):
///   Body, Head, Beard, Hair, Fur, Tattoo_Body, Tattoo_Head.
///
/// Apparel is handled separately via <see cref="ApparelGraphicRecordGetter_HoloShader_Patch"/>
/// because it goes through a different (non-node) helper entirely.
/// </summary>
[HarmonyPatch]
public static class PawnRenderNode_Subclass_GraphicFor_HoloShader_Patch
{
    private static readonly System.Type[] TargetSubclasses =
    {
        typeof(PawnRenderNode_Body),
        typeof(PawnRenderNode_Head),
        typeof(PawnRenderNode_Beard),
        typeof(PawnRenderNode_Hair),
        typeof(PawnRenderNode_Fur),
        typeof(PawnRenderNode_Tattoo_Body),
        typeof(PawnRenderNode_Tattoo_Head),
    };

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (System.Type t in TargetSubclasses)
        {
            MethodInfo m = AccessTools.DeclaredMethod(t, nameof(PawnRenderNode.GraphicFor), new[] { typeof(Pawn) });
            if (m != null)
                yield return m;
        }
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (__result == null) return;
        if (!MSSFPHoloUtil.IsHolo(pawn)) return;
        __result = HoloGraphicSwap.SwapToHoloShader(__result, pawn);
    }
}
