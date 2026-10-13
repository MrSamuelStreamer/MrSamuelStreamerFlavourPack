using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Post-generation guard for viewer-structure maps: no pawn a structure generated may belong to the
/// player faction. KCSG's GeneratePawnForContainer fills cryptosleep caskets using map.ParentFaction,
/// and the imported GoldenDuck layouts ship casket symbols with containPawnKindForPlayerAnyOf set — so
/// on any map whose ParentFaction is the player those occupants generate as colonists and join on load.
///
/// Safe to run map-wide: at PostGenerate time the player's own caravan/settle pawns have not been placed
/// yet (map entry runs after MapGenerator completes), so any Faction.OfPlayer pawn present was created
/// by structure generation.
/// </summary>
public static class StructureMapPawnGuard
{
    /// <summary>Odds the map's evicted occupants come out hostile rather than neutral. One roll per map,
    /// mirroring GenStep_MSSPointOfInterest's fallback disposition so a map reads consistently.</summary>
    private const float HostileChance = 0.5f;

    public static void EvictPlayerFactionPawns(Map map)
    {
        List<Pawn> pawns = new();
        // Recurses map.GetChildHolders (spawned caskets included) and applies the request to contained
        // things, so unspawned casket occupants are caught alongside any freely-spawned pawns.
        ThingOwnerUtility.GetAllThingsRecursively(
            map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), pawns,
            allowUnreal: true, passCheck: null, alsoGetSpawnedThings: true);

        Faction reassign = Rand.Chance(HostileChance) ? Faction.OfAncientsHostile : Faction.OfAncients;

        foreach (Pawn pawn in pawns)
        {
            if (pawn.Faction == Faction.OfPlayer)
                pawn.SetFaction(reassign);
        }
    }
}
