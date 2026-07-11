using HarmonyLib;
using MSSFP.Compatibility.BigAndSmall;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="Pawn.SpawnSetup"/> that repairs humanlike trackers on MSSFP
/// sapient ravens whenever they spawn — including the <c>respawningAfterLoad</c> path
/// when a save authored before the in-incident repair shipped is reloaded.
///
/// The B&amp;S sapient-animal morph does NOT repoint <c>pawn.def</c>: the pawn keeps the
/// animal <c>MSSFP_Raven</c> ThingDef (older B&amp;S builds used <c>HL_MSSFP_Raven</c>, so
/// both are accepted). A cheap defName compare filters out every non-raven pawn first; a
/// raven is then confirmed to be the <em>sapient</em> variant by the presence of the B&amp;S
/// race hediff <c>HL_MSSFP_Raven_RaceHediff</c>, so genuine wild-animal ravens are left as
/// animals and untouched.
///
/// See <see cref="SapientRaven_TrackerRepair"/> for the cascade analysis and the list of
/// trackers being repaired.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
public static class Pawn_SpawnSetup_SapientRavenRepair
{
    private const string SapientRavenRaceHediff = "HL_MSSFP_Raven_RaceHediff";
    private static HediffDef _raceHediff;

    public static void Postfix(Pawn __instance)
    {
        if (__instance?.def == null) return;

        // Cheap pre-filter: only ravens get past here, so every other pawn costs two string
        // compares. The morph keeps the animal defName; accept the legacy humanlike one too.
        string dn = __instance.def.defName;
        if (dn != "MSSFP_Raven" && dn != "HL_MSSFP_Raven") return;

        // Confirm this raven is the sapient variant (carries the B&S race hediff); leave real
        // wild-animal ravens alone.
        _raceHediff ??= DefDatabase<HediffDef>.GetNamedSilentFail(SapientRavenRaceHediff);
        if (_raceHediff == null || __instance.health?.hediffSet == null) return;
        if (!__instance.health.hediffSet.HasHediff(_raceHediff)) return;

        SapientRaven_TrackerRepair.EnsureHumanlikeTrackers(__instance);
    }
}
