using HarmonyLib;
using MSSFP.Buildings;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Hook <see cref="Frame.CompleteConstruction"/> for <see cref="Frame_AICoreLoaded"/>
/// instances. Vanilla's method is non-virtual, so we can't override it cleanly on the
/// subclass — we use Harmony to bracket the call with extract/deposit steps.
///
/// PREFIX (before vanilla runs):
///   - Capture frame Position + Map into local refs that survive the upcoming Frame.Destroy
///     inside vanilla's body.
///   - Call <see cref="Frame_AICoreLoaded.ExtractPawnFromLoadedCore"/> — pulls the Pawn out
///     of the loaded core in resourceContainer into the subclass's scribed pawnInTransit
///     ThingOwner. The loaded core is then Vanish-destroyed.
///   - Returns true (don't skip vanilla) — vanilla handles steel/components destruction,
///     orb spawn, lord notifications, etc.
///
/// POSTFIX (after vanilla):
///   - Look up the spawned orb at the captured position.
///   - Call <see cref="Frame_AICoreLoaded.DepositPawnToOrb"/> to transfer the Pawn from
///     pawnInTransit into the orb's <see cref="MSSFP.Holo.CompHoloProjector.stored"/> and
///     set the persona via <see cref="MSSFP.Comps.CompTrueAICore.SetPersonality"/>.
///
/// Mid-construction save survives because pawnInTransit is scribed on the subclass — even
/// if Pawn extraction has happened but deposit hasn't (very narrow window), reload restores
/// the Pawn in the Frame's container ready for next CompleteConstruction call.
/// </summary>
[HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
public static class Frame_CompleteConstruction_LoadedCore_Patch
{
    // Per-call stash so the postfix can find what the prefix captured. Harmony state passing
    // via static field is fine — Frame.CompleteConstruction is not reentrant on the same
    // frame instance.
    [System.ThreadStatic]
    private static Frame_AICoreLoaded pendingFrame;
    [System.ThreadStatic]
    private static IntVec3 pendingPos;
    [System.ThreadStatic]
    private static Map pendingMap;

    /// <summary>
    /// True while we are inside a Frame_AICoreLoaded.CompleteConstruction call. Set in the
    /// prefix, cleared in the postfix.
    ///
    /// <see cref="MSSFP.Comps.CompTrueAICore.PostSpawnSetup"/> checks this to suppress the
    /// "pick a persona" forced modal when the freshly-spawned orb is going to receive its
    /// persona from a loaded core in the same call frame.
    ///
    /// Why this is required: <see cref="Verse.LongEventHandler.ExecuteWhenFinished"/> runs
    /// SYNCHRONOUSLY when no long event is pending (the typical mid-tick construction case).
    /// So <c>PostSpawnSetup</c>'s queued popup actually fires inside <c>GenSpawn.Spawn</c>
    /// BEFORE this patch's postfix transfers the persona, with no way to defer it via the
    /// LongEventHandler API. A direct flag check at the popup site is the only race-free fix.
    /// </summary>
    [System.ThreadStatic]
    public static bool ConstructingLoaded;

    [HarmonyPrefix]
    public static void Prefix(Frame __instance)
    {
        if (__instance is not Frame_AICoreLoaded fl) return;
        pendingFrame = fl;
        pendingPos = fl.Position;
        pendingMap = fl.Map;
        ConstructingLoaded = true;
        fl.ExtractPawnFromLoadedCore();
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (pendingFrame == null)
        {
            ConstructingLoaded = false;
            return;
        }
        try
        {
            Frame_AICoreLoaded.DepositPawnToOrb(
                pendingMap,
                pendingPos,
                pendingFrame.pawnInTransit,
                pendingFrame.inTransitPersonality
            );
        }
        finally
        {
            pendingFrame = null;
            pendingPos = IntVec3.Invalid;
            pendingMap = null;
            ConstructingLoaded = false;
        }
    }
}
