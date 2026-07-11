using HarmonyLib;
using MSSFP.Compatibility.BigAndSmall;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="Pawn.SpawnSetup"/> that repairs humanlike trackers on MSSFP
/// sapient ravens whenever they spawn — including the <c>respawningAfterLoad</c> path
/// when a save authored before the in-incident repair shipped is reloaded.
///
/// A raven that went through the creepjoiner incident is either the successfully-swapped
/// humanlike (<c>HL_MSSFP_Raven</c>) or — when B&amp;S's swap returned null — the raw animal
/// (<c>MSSFP_Raven</c>) with humanlike trackers bolted on by the incident. Both defNames are
/// accepted. A genuine wild-animal raven is distinguished by having no skills tracker
/// (<c>skills == null</c>): animals never get one, so a non-null <c>skills</c> means this is a
/// pseudo-colonist raven that needs the full humanlike tracker set. This is deliberately NOT
/// gated on the <c>HL_MSSFP_Raven_RaceHediff</c> hediff — a failed-swap animal never received
/// it, yet is exactly the pawn that NREs.
///
/// See <see cref="SapientRaven_TrackerRepair"/> for the cascade analysis and the list of
/// trackers being repaired.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
public static class Pawn_SpawnSetup_SapientRavenRepair
{
    public static void Postfix(Pawn __instance)
    {
        if (__instance?.def == null) return;

        // Cheap pre-filter: only ravens get past here, so every other pawn costs two string
        // compares. Accept both the swapped humanlike def and the raw animal def.
        string dn = __instance.def.defName;
        if (dn != "MSSFP_Raven" && dn != "HL_MSSFP_Raven") return;

        // A real wild-animal raven has no skills tracker — leave it as an animal. A non-null
        // skills tracker marks a pseudo-colonist raven that needs its humanlike trackers repaired.
        if (__instance.skills == null) return;

        SapientRaven_TrackerRepair.EnsureHumanlikeTrackers(__instance);
    }
}
