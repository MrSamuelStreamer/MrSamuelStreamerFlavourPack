using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MSSFP.Tunnels.Incidents;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Patches that fix the TunnelCaravan insect-attack encounter.
///
/// Part 1a – <see cref="CaravanIncidentUtility_Patch.UsesCaveMapForTunnelCaravan"/>:
///   Intercepts <see cref="CaravanIncidentUtility.GetOrGenerateMapForIncident"/> and
///   swaps the suggested world-object def to <c>MSSFP_TunnelEncounterSite</c>, whose
///   map generator produces cave terrain instead of an open field.
///
/// Part 1b – <see cref="CaravanIncidentUtility_Patch.EnsureStateComponentOnEncounterMap"/>:
///   Postfix on the same method.  After the encounter map is generated, ensures
///   <see cref="MapComponent_TunnelCaravanState"/> is present on it.
///   <c>customMapComponents</c> in MapGeneratorDef is not always processed for
///   caravan-incident maps, so we add the component here as a guaranteed fallback.
///
/// Part 2 – <see cref="ExitMapAndCreateCaravan_TunnelPatch"/>:
///   Uses TargetMethods() to patch ALL overloads of ExitMapAndCreateCaravan so we
///   are not tied to a specific parameter signature (which varies across RimWorld
///   versions).  When reform fires after a tunnel fight, replaces the plain Caravan
///   with a properly-configured TunnelCaravan using state from TunnelEncounterSetup
///   (primary) or MapComponent_TunnelCaravanState (save/load fallback).
///
/// Part 3 – <see cref="MapParent_Patch"/>:
///   Clears HasActiveEncounter when the encounter map is removed, preventing stale
///   state from affecting later caravan operations if the player never reforms.
/// </summary>
[HarmonyPatch(typeof(CaravanIncidentUtility))]
public static class CaravanIncidentUtility_Patch
{
    // ── Part 1a ────────────────────────────────────────────────────────────

    [HarmonyPatch(nameof(CaravanIncidentUtility.GetOrGenerateMapForIncident))]
    [HarmonyPrefix]
    public static void UsesCaveMapForTunnelCaravan(
        Caravan caravan,
        ref WorldObjectDef suggestedMapParentDef)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (caravan is not TunnelCaravan) return;

        // Cave-in uses a dedicated WorldObjectDef whose MapGeneratorDef runs
        // GenStep_TunnelCaveInLayout (deterministic tunnel + barrier layout).
        // All other tunnel incidents use the generic MSSFP_TunnelEncounterSite
        // (random cave corridors via GenStep_TunnelCaravanCaves).
        suggestedMapParentDef = IncidentWorker_TunnelCaravanCaveIn.IsCaveIn
            ? DefDatabase<WorldObjectDef>.GetNamed("MSSFP_TunnelCaveInSite")
            : DefDatabase<WorldObjectDef>.GetNamed("MSSFP_TunnelEncounterSite");
    }

    // ── Part 1b ────────────────────────────────────────────────────────────

    /// <summary>
    /// Postfix on GetOrGenerateMapForIncident.  By the time this runs the encounter
    /// map is fully generated and TunnelEncounterSetup.PendingCaravan is still set.
    /// If customMapComponents already created the component we skip; otherwise we
    /// create it here so it is available as a save/load fallback.
    /// NOTE: __result may not be Map if the method signature differs; if the cast
    /// fails the parameter will be null and we skip safely.
    /// </summary>
    [HarmonyPatch(typeof(CaravanIncidentUtility))]
    [HarmonyPatch(nameof(CaravanIncidentUtility.GetOrGenerateMapForIncident))]
    [HarmonyPostfix]
    public static void EnsureStateComponentOnEncounterMap(Caravan caravan, Map __result)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (caravan is not TunnelCaravan) return;
        if (__result == null) return;
        if (__result.GetComponent<MapComponent_TunnelCaravanState>() != null) return;

        var comp = new MapComponent_TunnelCaravanState(__result);
        __result.components.Add(comp);
    }
}
// ── Part 3 ─────────────────────────────────────────────────────────────
[HarmonyPatch(typeof(MapParent))]
public static class MapParent_Patch
{
    [HarmonyPatch(nameof(MapParent.Notify_MyMapRemoved))]
    [HarmonyPostfix]
    public static void ClearActiveEncounterStateOnMapRemoval(MapParent __instance)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (__instance.def?.defName == "MSSFP_TunnelEncounterSite"
            || __instance.def?.defName == "MSSFP_TunnelCaveInSite")
            TunnelEncounterSetup.HasActiveEncounter = false;
    }
}

/// <summary>
/// Patches ALL overloads of CaravanExitMapUtility.ExitMapAndCreateCaravan that
/// return Caravan, using TargetMethods() so we are not bound to a specific signature.
///
/// When a tunnel encounter is active (TunnelEncounterSetup.HasActiveEncounter), the
/// postfix replaces the resulting plain Caravan with a TunnelCaravan carrying the
/// original journey's travel data.
///
/// Two data sources are tried in order:
///  1. TunnelEncounterSetup static fields  — set before the fight, valid until reform
///  2. MapComponent_TunnelCaravanState     — save/load fallback, read in prefix while
///                                           pawn.MapHeld is still non-null
/// </summary>
[HarmonyPatch]
public static class ExitMapAndCreateCaravan_TunnelPatch
{
    private static MapComponent_TunnelCaravanState _capturedComponent;

    static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(CaravanExitMapUtility)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "ExitMapAndCreateCaravan" && m.ReturnType == typeof(Caravan));
    }

    /// <summary>
    /// Runs before any ExitMapAndCreateCaravan overload.  At this point pawns are
    /// still on the encounter map (MapHeld is non-null), so we can read the map
    /// component for the save/load fallback case.
    /// </summary>
    [HarmonyPrefix]
    public static void CaptureMapComponent(object[] __args)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        _capturedComponent = null;

        if (__args?.Length > 0 && __args[0] is IEnumerable<Pawn> pawns)
        {
            Pawn first = pawns.FirstOrDefault();
            if (first?.MapHeld != null)
                _capturedComponent = first.MapHeld.GetComponent<MapComponent_TunnelCaravanState>();
        }
    }

    [HarmonyPostfix]
    public static void ReplaceWithTunnelCaravan(ref Caravan __result)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        // Primary source: static set in TryExecuteWorker (survives caravan destruction).
        // Also verify tile matches to avoid converting unrelated caravans if another
        // tunnel encounter was somehow still active in the background.
        bool hasStaticData = TunnelEncounterSetup.HasActiveEncounter
                             && __result != null
                             && __result.Tile == TunnelEncounterSetup.ActiveEncounterTile;

        // Fallback: map component (valid after save/load when statics are gone)
        bool hasComponentData = _capturedComponent?.hasData == true;

        MapComponent_TunnelCaravanState comp = _capturedComponent;
        _capturedComponent = null;

        if ((!hasStaticData && !hasComponentData) || __result is TunnelCaravan)
            return;

        // Read from static first; fall back to component for save/load scenarios
        PlanetTile    origin      = hasStaticData ? TunnelEncounterSetup.ActiveOrigin             : comp.origin;
        PlanetTile    destination = hasStaticData ? TunnelEncounterSetup.ActiveDestination        : comp.destination;
        Building_Tunnel tunnel    = hasStaticData ? TunnelEncounterSetup.ActiveTunnel             : comp.tunnel;
        int           startTick  = hasStaticData ? TunnelEncounterSetup.ActiveTravelStartsAtTick  : comp.travelStartsAtTick;
        int           endTick    = hasStaticData ? TunnelEncounterSetup.ActiveTravelEndsAtTick    : comp.travelEndsAtTick;

        TunnelEncounterSetup.HasActiveEncounter = false;

        Caravan old = __result;

        TunnelCaravan tc = (TunnelCaravan)WorldObjectMaker.MakeWorldObject(
            MSSFPDefOf.MSSFP_TunnelCaravanWorldObject);
        tc.Tile               = old.Tile;
        tc.SetFaction(old.Faction);
        tc.origin             = origin;
        tc.destination        = destination;
        tc.tunnel             = tunnel;
        tc.travelStartsAtTick = startTick;
        tc.travelEndsAtTick   = endTick;

        foreach (Pawn pawn in old.PawnsListForReading.ToList())
        {
            old.RemovePawn(pawn);
            tc.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
        }

        old.Destroy();
        Find.WorldObjects.Add(tc);

        tc.pather.StartPath(tc.destination.tileId, null, repathImmediately: true);

        __result = tc;
    }
}
