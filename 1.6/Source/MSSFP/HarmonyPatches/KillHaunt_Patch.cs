using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Haunts;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// When a player colonist kills a humanlike pawn, there is a chance the killer
/// becomes haunted by the victim's spirit (a bad dynamic haunt).
/// Probability diminishes with the number of active bad haunts on the killer.
/// </summary>
[HarmonyPatch(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled))]
public static class KillHaunt_Patch
{
    /// <summary>
    /// Per-pawn cooldown tracker (thingIDNumber → tick when last kill haunt was applied).
    /// Runtime-only — not saved. Resets on game load, which is fine since the cooldown
    /// is meant to throttle rapid kills within a single session (e.g. raids).
    /// </summary>
    private static readonly Dictionary<int, int> lastKillHauntTick = new();

    public static void Postfix(Pawn killed, Pawn killer)
    {
        if (!MSSFPMod.settings.EnableKillHaunts)
            return;

        if (killer == null || killed == null)
            return;

        // Only player colonists can gain kill haunts
        if (!killer.IsColonist || !killer.RaceProps.Humanlike)
            return;

        // Only humanlike victims generate kill haunts
        if (!killed.RaceProps.Humanlike)
            return;

        // Don't haunt with your own colonists (friendly fire edge case)
        if (killed.Faction == killer.Faction)
            return;

        // Resistant pawns are excluded
        if (killer.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntResistant) == true)
            return;

        // Per-pawn cooldown — prevent haunt spam during raids
        int now = Find.TickManager.TicksGame;
        int killerId = killer.thingIDNumber;
        if (lastKillHauntTick.TryGetValue(killerId, out int lastTick)
            && (now - lastTick) < MSSFPMod.settings.KillHauntCooldownTicks)
            return;

        // Count active bad haunts on the killer for diminishing returns + cap check
        int activeBadHaunts = 0;
        foreach (Hediff hediff in killer.health.hediffSet.hediffs)
        {
            HediffComp_Haunt hauntComp = hediff.TryGetComp<HediffComp_Haunt>();
            if (hauntComp != null && !hauntComp.Props.isGood)
                activeBadHaunts++;
        }

        // Hard cap on concurrent bad haunts per pawn
        if (activeBadHaunts >= MSSFPMod.settings.MaxBadHauntsPerPawn)
            return;

        // chance = baseChance / (1 + activeBadHaunts)
        float chance = MSSFPMod.settings.KillHauntBaseChance / (1f + activeBadHaunts);

        // Sensitive pawns are more susceptible
        if (killer.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntSensitive) == true)
            chance *= 1.5f;

        if (!Rand.Chance(chance))
            return;

        lastKillHauntTick[killerId] = now;

        Hediff hediffNew = killer.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayerBad);
        hediffNew.Severity = 0.05f;

        if (hediffNew.TryGetComp(out HediffComp_Haunt comp))
        {
            comp.SetPawnToDraw(killed);
        }

        HauntProfile profile = HauntProfileBuilder.TryBuild(killed, isGood: false);
        if (hediffNew.TryGetComp(out HediffComp_DynamicHaunt dynamicComp) && profile != null)
        {
            dynamicComp.SetProfile(profile);
        }

        Find.LetterStack.ReceiveLetter(
            "MSS_FP_KillHaunt_Letter_Label".Translate(),
            "MSS_FP_KillHaunt_Letter_Text".Translate(
                killed.LabelShort,
                killer.LabelShort
            ),
            LetterDefOf.ThreatSmall,
            killer
        );
    }
}
