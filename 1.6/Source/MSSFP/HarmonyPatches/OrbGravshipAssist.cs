using System.Collections.Generic;
using MSSFP.Buildings;
using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Detection helper for "is a powered, persona-loaded Pondering Orb sitting on this
/// gravship's substructure right now?". Shared by the GravshipRange StatPart and the
/// ConsumeFuel cooldown postfix so both effects fire under the exact same predicate.
///
/// Hot-path: <see cref="Building_GravEngine.MaxLaunchDistance"/> is read every frame
/// during the world tile-picker. To keep cost flat:
///   - Iterate <see cref="ListerBuildings.allBuildingsColonist"/> (small list) and type-test
///     against <see cref="Building_AICore"/> — cheaper than scanning each substructure cell's
///     <c>GetThingList</c>, and catches every persona subclass without per-def enumeration.
///   - Read the substructure via <see cref="Building_GravEngine.AllConnectedSubstructureNoRegen"/>
///     so the read doesn't force a SectionLayer regen.
///   - Cache the resolved orb per <c>(engineThingID, GenTicks.TicksGame)</c>. Same-tick repeat
///     calls hit the cache. Key is deterministic (no wall-clock) so the cache is safe under
///     the RimWorld Multiplayer mod.
/// </summary>
public static class OrbGravshipAssist
{
    // MP-safe: deterministic key, no wall-clock. (engineThingID) -> (tick the entry is valid for, resolved orb or null).
    private static readonly Dictionary<int, (int tick, CompTrueAICore orb)> Cache = new();

    /// <summary>
    /// True iff <paramref name="engine"/> has at least one <see cref="Building_AICore"/> on its
    /// connected substructure with power on and an active persona loaded.
    /// </summary>
    public static bool TryGetActiveAssistOrb(Building_GravEngine engine, out CompTrueAICore orb)
    {
        orb = null;
        if (engine == null || !engine.Spawned || engine.Map == null)
            return false;

        int now = GenTicks.TicksGame;
        int key = engine.thingIDNumber;
        if (Cache.TryGetValue(key, out var hit) && hit.tick == now)
        {
            orb = hit.orb;
            return orb != null;
        }

        HashSet<IntVec3> subs = engine.AllConnectedSubstructureNoRegen;
        foreach (Building b in engine.Map.listerBuildings.allBuildingsColonist)
        {
            if (b is not Building_AICore aiCore)
                continue;
            if (!aiCore.Spawned)
                continue;
            if (!subs.Contains(aiCore.Position))
                continue;
            CompTrueAICore core = aiCore.TryGetComp<CompTrueAICore>();
            if (core?.activePersonality == null)
                continue;
            CompPowerTrader power = aiCore.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
                continue;
            orb = core;
            break;
        }

        Cache[key] = (now, orb);
        return orb != null;
    }
}
