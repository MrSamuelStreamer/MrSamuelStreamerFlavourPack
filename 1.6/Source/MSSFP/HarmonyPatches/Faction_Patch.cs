using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

// Option 3: Prevent TryGenerateNewLeader from silently replacing a living leader.
// The only legitimate replacement paths are death (leader.Dead) and capture
// (leader.IsPrisoner). Everything else keeps the same character as faction leader,
// so that the pawn injected into raids by IncidentWorker_Raid_Patch remains the
// persistent face of that faction.
[HarmonyPatch(typeof(Faction))]
public static class Faction_Patch
{
    [HarmonyPatch("TryGenerateNewLeader")]
    [HarmonyPrefix]
    private static bool PreserveExistingLeader(Faction __instance, ref bool __result)
    {
        if (!MSSFPMod.settings.OverrideFactionLeaderSpawn) return true;
        // Respect faction defs that always want fresh leaders (e.g. player faction).
        if (__instance.def.leaderForceGenerateNewPawn) return true;
        if (__instance.IsPlayer) return true;

        Pawn leader = __instance.leader;
        if (leader == null || leader.Dead || leader.Destroyed) return true;
        // Let vanilla replace a captured or enslaved leader.
        if (leader.IsPrisoner || leader.IsSlaveOfColony) return true;

        // Leader is alive and free — keep them.
        __result = true;
        return false;
    }
}
