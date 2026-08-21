using System.Collections.Generic;
using System.Linq;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: a section of tunnel ceiling collapses, sealing the passage with fallen rock.
///
/// Map layout (built by GenStep_TunnelCaveInLayout during map generation):
///
///   ┌─────────────────────────────┐
///   │ solid rock   │ tunnel (5w) │ solid rock                         │
///   │              │             │                                     │
///   │              │  open       │  ← approach from south             │
///   │              │             │                                     │
///   │              │═════════════│  ← lower barrier (5 deep, @±20)    │
///   │              │             │                                     │
///   │              │  enclosed   │  ← pawns teleported here           │
///   │              │  section    │    (CaravanEnterMapUtility places   │
///   │              │             │     them at edge; PostSetup moves   │
///   │              │             │     them to the enclosed section)   │
///   │              │═════════════│  ← upper barrier (5 deep, @±20)    │
///   │              │             │                                     │
///   │              │  open       │  ← approach to north               │
///   └─────────────────────────────┘
///
/// Gen pipeline (MSSFP_TunnelCaveIn MapGeneratorDef):
///   ElevationFertility (10)         — elevation data
///   MSSFP_TunnelCaveSetup (230)     — cave floor terrain + thick roof everywhere
///   MSSFP_TunnelCaveInLayout (240)  — deterministic rock layout + loot scatter
///   FindPlayerStartSpot (850)       — finds open cell in approach section
///   RockChunks (970)                — loose debris for flavour
///   MutatorFinal (1600) / Fog
///
/// IsCaveIn flag: set in DoNonCombatExecute (before SetupCaravanAttackMap), read by
/// the (commit 4) caravan-incident-utility patch to route this incident to
/// MSSFP_TunnelCaveInSite (which uses the MSSFP_TunnelCaveIn generator) instead of the
/// generic MSSFP_TunnelEncounterSite used by all other tunnel incidents.
///
/// Success condition: all mobile player pawns have an unobstructed walking path to
/// any map edge cell (checked by MapComponent_CaveInBlocker every 60 ticks).
/// Reform is blocked by the (commit 4) reform-blocking patch until IsCleared is true.
/// </summary>
public class IncidentWorker_TunnelCaravanCaveIn : IncidentWorker_TunnelCaravanNonCombat
{
    /// <summary>
    /// Set to true for the duration of DoNonCombatExecute so the (commit 4) Harmony
    /// prefix can route this incident to MSSFP_TunnelCaveInSite (which uses the
    /// MSSFP_TunnelCaveIn map generator) rather than the generic MSSFP_TunnelEncounterSite
    /// used by all other tunnel incidents.
    /// </summary>
    public static bool IsCaveIn;

    /// <summary>
    /// Wraps the base execute with the routing flag so SetupCaravanAttackMap sees it.
    /// </summary>
    protected override void DoNonCombatExecute(IncidentParms parms)
    {
        IsCaveIn = true;
        try
        {
            base.DoNonCombatExecute(parms);
        }
        finally
        {
            IsCaveIn = false;
        }
    }

    /// <summary>
    /// Called after the encounter map is generated and CaravanEnterMapUtility.Enter
    /// has placed pawns (via edge-entry) in the south approach section. Teleports all
    /// player pawns into the enclosed centre section, then activates the blocker.
    /// Layout and loot were already placed by GenStep_TunnelCaveInLayout.
    /// </summary>
    protected override void PostSetupEncounterMap(Map map)
    {
        int centreX = map.Size.x / 2;
        int centreZ = map.Size.z / 2;

        PlacePawnsInCentre(map, centreX, centreZ);

        var blocker = new MapComponent_CaveInBlocker(map);
        blocker.Activate();
        map.components.Add(blocker);
    }

    // ── Pawn relocation ────────────────────────────────────────────────────────

    private static void PlacePawnsInCentre(Map map, int centreX, int centreZ)
    {
        List<Pawn> playerPawns = map.mapPawns.AllPawnsSpawned
            .Where(p => p.Faction != null && p.Faction == Faction.OfPlayer)
            .ToList();

        if (playerPawns.Count == 0) return;

        // Enclosed section: tunnel cells between the inner edges of the two barriers.
        // Uses the same geometry constants as GenStep_TunnelCaveInLayout.
        int enclosedLo = centreZ - GenStep_TunnelCaveInLayout.BarrierOffset + 1;
        int enclosedHi = centreZ + GenStep_TunnelCaveInLayout.BarrierOffset - 1;
        int halfWidth = GenStep_TunnelCaveInLayout.TunnelHalfWidth;

        List<IntVec3> candidates = new List<IntVec3>();
        for (int x = centreX - halfWidth; x <= centreX + halfWidth; x++)
        for (int z = enclosedLo; z <= enclosedHi; z++)
        {
            var cell = new IntVec3(x, 0, z);
            if (cell.InBounds(map))
                candidates.Add(cell);
        }
        candidates.Shuffle();

        int idx = 0;
        foreach (Pawn pawn in playerPawns)
        {
            if (idx >= candidates.Count)
            {
                Log.Warning($"[MSSFP] CaveIn: no candidate cell left for {pawn.LabelShort}");
                break;
            }

            pawn.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(pawn, candidates[idx++], map, WipeMode.VanishOrMoveAside);
        }
    }
}
