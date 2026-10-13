using System.Linq;
using MSSFP.Comps;
using MSSFP.Defs;
using MSSFP.Holo;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// Custom Frame for the loaded-variant Pondering Orb. Holds a scribed
/// <see cref="pawnInTransit"/> ThingOwner so the holo Pawn survives mid-construction save/load
/// during the brief window between extracting from the loaded core and depositing into the
/// new orb's projector (see DA pass concern 1).
///
/// Frame-class swap is wired via Harmony postfix on
/// <see cref="RimWorld.ThingDefGenerator_Buildings.NewFrameDef_Thing"/> in
/// <c>ThingDefGenerator_FrameClassSwap_Patch</c> — vanilla generates frame defs with
/// <c>thingClass = typeof(Frame)</c> hard-coded, so we postfix-replace it for the loaded
/// variant.
///
/// CompleteConstruction is non-virtual in vanilla, so the actual extract/deposit hooks live
/// in <see cref="MSSFP.HarmonyPatches.Frame_CompleteConstruction_LoadedCore_Patch"/>. This
/// subclass provides the storage container + helper methods those patches call.
/// </summary>
public class Frame_AICoreLoaded : Frame
{
    /// <summary>
    /// Save-safe handoff container. Pawn lives here from
    /// <see cref="ExtractPawnFromLoadedCore"/> until
    /// <see cref="DepositPawnToOrb"/> succeeds.
    /// </summary>
    public ThingOwner<Pawn> pawnInTransit;

    /// <summary>Persona def captured at extraction time, applied to the new orb on deposit.</summary>
    public AIPersonalityDef inTransitPersonality;

    public Frame_AICoreLoaded()
    {
        pawnInTransit = new ThingOwner<Pawn>(this, oneStackOnly: false);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref pawnInTransit, "pawnInTransit", this);
        Scribe_Defs.Look(ref inTransitPersonality, "inTransitPersonality");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && pawnInTransit == null)
        {
            pawnInTransit = new ThingOwner<Pawn>(this, oneStackOnly: false);
        }
    }

    /// <summary>
    /// Pull the Pawn + persona out of any loaded persona core sitting in
    /// <see cref="Frame.resourceContainer"/>, into our scribed handoff containers. Then
    /// remove the loaded core from resourceContainer (Vanish destroy) so vanilla's
    /// ClearAndDestroyContents doesn't try to destroy a container with a live Pawn inside.
    ///
    /// Called from the Frame.CompleteConstruction prefix BEFORE vanilla runs its content
    /// cleanup. Idempotent — running twice (e.g. if a prefix re-fires) is a no-op after
    /// the first call.
    /// </summary>
    public void ExtractPawnFromLoadedCore()
    {
        if (pawnInTransit != null && pawnInTransit.Count > 0) return; // already extracted

        for (int i = resourceContainer.Count - 1; i >= 0; i--)
        {
            Thing t = resourceContainer[i];
            CompLoadedAIPersonaCore loaded = t.TryGetComp<CompLoadedAIPersonaCore>();
            if (loaded == null) continue;

            inTransitPersonality = loaded.storedPersonality;
            if (loaded.storedHolo != null && loaded.storedHolo.Count > 0)
            {
                loaded.storedHolo.TryTransferAllToContainer(pawnInTransit);
            }
            // Vanish removes without leavings and respects ThingOwner ownership chain.
            resourceContainer.Remove(t);
            t.Destroy(DestroyMode.Vanish);
            break; // costList only allows one loaded core per frame
        }
    }

    /// <summary>
    /// Deposit the in-transit Pawn into the freshly-spawned orb's projector. Called from
    /// the Frame.CompleteConstruction postfix using the position captured before vanilla
    /// destroyed this Frame.
    ///
    /// If the orb can't be located (vanilla error, mod conflict, off-map spawn), the Pawn
    /// stays in pawnInTransit and is logged loudly. A future hook (e.g. CompTick scan) can
    /// recover it — saved as a known follow-up rather than silent data loss.
    /// </summary>
    public static void DepositPawnToOrb(Map map, IntVec3 pos, ThingOwner<Pawn> sourcePawns, AIPersonalityDef persona)
    {
        if (map == null || !pos.IsValid)
        {
            Log.Error("[MSSFP] DepositPawnToOrb: invalid map/pos — pawn stranded in transit.");
            return;
        }
        Building_AICore orb = map.thingGrid
            .ThingsListAtFast(pos)
            .OfType<Building_AICore>()
            .FirstOrDefault();
        if (orb == null)
        {
            Log.Error($"[MSSFP] DepositPawnToOrb: no Building_AICore at {pos} after Frame.CompleteConstruction. Pawn left in transit container.");
            return;
        }

        CompHoloProjector proj = orb.TryGetComp<CompHoloProjector>();
        if (proj != null && sourcePawns != null && sourcePawns.Count > 0)
        {
            sourcePawns.TryTransferAllToContainer(proj.stored);
        }

        CompTrueAICore core = orb.TryGetComp<CompTrueAICore>();
        if (core != null && persona != null)
        {
            core.SetPersonality(persona); // flips personaChosen via Phase 1 fix
        }
    }
}
