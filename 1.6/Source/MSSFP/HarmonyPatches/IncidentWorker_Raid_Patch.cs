using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

// Option 1: After a raid's pawns are generated and spawned, inject the faction's
// actual leader into the group at a configurable chance. The leader is spawned as-is
// (no redressing, no equipment changes) using the same arrival mode as the rest of
// the raid. Adding them to the pawns list before MakeLords runs means they join
// the lord and fight alongside the raid rather than standing idle.
//
// Lifecycle after injection:
//   - Leader survives: despawn calls PassToWorld, restoring FactionLeader situation.
//   - Leader dies: Notify_MemberDied -> Notify_LeaderDied -> TryGenerateNewLeader
//     (Faction_Patch does NOT block this because leader.Dead == true at that point).
[HarmonyPatch(typeof(IncidentWorker_Raid))]
public static class IncidentWorker_Raid_Patch
{
    [HarmonyPatch( "PostProcessSpawnedPawns")]
    [HarmonyPostfix]
    private static void InjectLeaderIntoRaid(IncidentParms parms, List<Pawn> pawns)
    {
        if (!MSSFPMod.settings.OverrideFactionLeaderSpawn) return;
        if (!Rand.Chance(MSSFPMod.settings.FactionLeaderRaidChance)) return;

        if (parms?.faction == null) return;
        if (parms.target is not Map map) return;
        if (parms.raidArrivalMode == null) return;
        if (!parms.spawnCenter.IsValid) return;

        // Only inject for hostile factions leading enemy raids.
        if (!parms.faction.HostileTo(Faction.OfPlayer)) return;

        Pawn leader = parms.faction.leader;
        if (leader == null || leader.Dead || leader.Destroyed) return;
        // Don't pull a leader who is already on a map (prisoner, quest pawn, etc.).
        if (leader.Spawned) return;
        // Don't break active quests that require this leader to be available.
        if (QuestUtility.IsReservedByQuestOrQuestBeingGenerated(leader)) return;
        // Sanity: don't add if already in the group somehow.
        if (pawns.Contains(leader)) return;

        // Spawn the leader using the same arrival mode and entry point as the raid.
        // Pawn.SpawnSetup automatically removes the pawn from WorldPawns; despawn
        // will PassToWorld them back when the raid ends.
        parms.raidArrivalMode.Worker.Arrive([leader], parms);
        pawns.Add(leader);

        ModLog.Debug($"[FactionLeader] {leader.LabelShort} ({parms.faction.Name}) joined their faction's raid.");
    }
}
