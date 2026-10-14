using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// When a pawn carrying <c>MSSFP_GoldenCubeImplant</c> dies, the cube has a chance
/// to teleport into the killing colonist or slave. The only player-facing signal is
/// a transient <c>Messages.Message("Ouch!")</c> over the new carrier — never a letter.
///
/// Eligibility (per design): killer must be a colonist or slave-of-colony, alive,
/// spawned, humanlike, and not already carrying the hediff. Failed rolls and
/// ineligible killers leave the hediff on the corpse so a doctor can still extract
/// the cube via <c>MSSFP_RemoveGoldenCubeImplant</c>.
/// </summary>
[HarmonyPatch(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled))]
public static class Pawn_Kill_GoldenCube_Patch
{
    public static void Postfix(Pawn killed, Pawn killer)
    {
        if (!ModsConfig.AnomalyActive) return;
        if (!MSSFPMod.settings.EnableGoldenCubeImplant) return;
        if (killed == null || killed.health == null) return;

        HediffDef cubeDef = MSSFPDefOf.MSSFP_GoldenCubeImplant;
        if (cubeDef == null) return;

        Hediff carried = killed.health.hediffSet.GetFirstHediffOfDef(cubeDef);
        if (carried == null) return;

        if (killer == null) return;
        if (killer.Dead || killer.Destroyed) return;
        if (!killer.Spawned) return;
        if (!killer.RaceProps.Humanlike) return;
        if (!killer.IsColonist && !killer.IsSlaveOfColony) return;
        if (killer.health == null) return;
        if (killer.health.hediffSet.HasHediff(cubeDef)) return;

        if (!Rand.Chance(MSSFPMod.settings.GoldenCubeTransferChance)) return;

        // Transfer: remove from victim first so the map-effect comp on the corpse
        // hediff stops ticking before the killer's copy starts. Anchor on Torso so
        // the surgery recipe can find a body part to operate on.
        killed.health.RemoveHediff(carried);

        BodyPartRecord torso = killer.health.hediffSet
            .GetNotMissingParts()
            .FirstOrDefault(p => p.def == BodyPartDefOf.Torso);
        killer.health.AddHediff(cubeDef, torso);

        Messages.Message(
            "MSSFP_GoldenCubeOuch_Msg".Translate(),
            new LookTargets(killer),
            MessageTypeDefOf.NeutralEvent,
            historical: false);
    }
}
