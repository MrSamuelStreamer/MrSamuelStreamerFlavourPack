using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Cached reflection handle for the private <c>Pawn_DraftController.pawn</c> back-reference.
/// Shared across both patches in this file.
/// </summary>
internal static class DraftControllerReflection
{
    public static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_DraftController), "pawn");
}

/// <summary>
/// Holos cannot be drafted. Patches the <see cref="Pawn_DraftController.Drafted"/> setter to
/// no-op when the target pawn is a projection AND the caller is trying to draft (value=true).
/// Always allow undraft (value=false) — otherwise a holo that latched into drafted via any
/// side path (pre-fix save, third-party mod, lord transfer, dev tool) gets stuck-drafted
/// permanently because the draft gizmo is hidden and the only undraft path runs through this
/// setter too. Asymmetric block: refuse to enter drafted state, always allow leaving it.
/// </summary>
[HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
public static class Pawn_DraftController_Drafted_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn_DraftController __instance, bool value)
    {
        if (!value)
            return true; // always permit undraft, even on holos
        Pawn pawn = DraftControllerReflection.PawnField.GetValue(__instance) as Pawn;
        return !MSSFPHoloUtil.IsHolo(pawn);
    }
}

/// <summary>
/// Hide the Draft toggle gizmo entirely for holo projections. The setter-block patch above
/// already neutralises the action, but a visible-but-inert button is worse than no button —
/// strip it from <see cref="Pawn_DraftController.GetGizmos"/> before it reaches the UI.
///
/// Vanilla returns an iterator-block IEnumerable&lt;Gizmo&gt;; we replace the whole result with
/// an empty enumerable when the pawn is a holo rather than filtering, because the controller
/// emits only draft-related commands (draft toggle, fire-at-will, stop-firing-at-will) and
/// none of them are meaningful for an undraftable holo.
/// </summary>
[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
public static class Pawn_DraftController_GetGizmos_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
    {
        Pawn pawn = DraftControllerReflection.PawnField.GetValue(__instance) as Pawn;
        if (MSSFPHoloUtil.IsHolo(pawn))
            __result = Enumerable.Empty<Gizmo>();
    }
}
