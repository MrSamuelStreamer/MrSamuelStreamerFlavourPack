using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Intercepts <see cref="Pawn.Kill"/> on holo pawns. Routes to
/// <see cref="CompHoloProjector.OnProjectionCollapsed"/> instead of allowing vanilla death.
///
/// Catches death-by-hediff (plague, asphyxia, vacuum, surgery-induced vital loss) and
/// external <c>Pawn.Kill</c> calls (dev gizmos, mods, quest cleanup) that bypass the
/// <see cref="CompHoloProjected.PostPreApplyDamage"/> absorb path.
///
/// COMPAT NOTE: returns false on Pawn.Kill for holo pawns. Other mods' Pawn.Kill postfixes
/// see <c>pawn.Dead == false</c>. Bounded blast radius — holo pawns are MSSFP-only.
/// Priority.First lets competing prefixes still observe the call but can't prevent our
/// intercept from running first.
///
/// BYPASS: <see cref="MSSFPHoloUtil.DestroyHoloForReal"/> increments a thread-local guard;
/// when set, this prefix forwards to vanilla. Projector PostDestroy cleanup uses that path
/// to actually destroy stored pawns.
///
/// SCRIBE GUARD: bails to vanilla during load/save to avoid manipulating half-loaded pawns
/// or firing letters when the letter stack isn't ready.
///
/// NO PARALLEL <c>Pawn.Destroy</c> PATCH: Vehicle Framework 1.6.x has a bug that triggers
/// when anything prefix-patches <see cref="Pawn.Destroy"/> with a <c>bool</c> return,
/// breaking the VEF parallel renderer (spam of "Trying to set material properties with no
/// main tex" + texture-load-on-worker-thread errors, eventually <c>ThreadAbortException</c>
/// inside <c>SmashTools.Performance.DedicatedThread.Execute</c>). Until VEF ships a fix,
/// the Destroy hook is removed and rare external <c>pawn.Destroy()</c> calls on a spawned
/// holo are caught by the projector tick-rare cleanup in
/// <see cref="CompHoloProjector.CompTickRare"/>.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Pawn_Kill_HoloIntercept_Patch
{
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
    {
        if (MSSFPHoloUtil.IsHoloDestroyBypassed) return true;
        if (Scribe.mode != LoadSaveMode.Inactive) return true;
        if (Current.Game == null) return true;
        if (!MSSFPHoloUtil.IsHolo(__instance)) return true;

        // Stored (non-spawned) holo: block the kill silently, no collapse letter.
        // Reaching here would mean some external code grabbed a reference to a stored pawn
        // and tried to kill it — block, no UX noise.
        if (!__instance.Spawned) return false;

        CompHoloProjector proj = __instance.TryGetComp<CompHoloProjected>()?.ProjectorComp;
        proj?.OnProjectionCollapsed(dinfo, exactCulprit);
        return false;
    }
}
