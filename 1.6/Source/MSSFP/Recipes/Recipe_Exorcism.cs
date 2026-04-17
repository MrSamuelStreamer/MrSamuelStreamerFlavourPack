using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps.Map;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Recipes;

/// <summary>
/// Medical operation to exorcise a named haunt from a pawn.
/// Outcome depends on the doctor's Medicine skill vs haunt severity:
///   Success  (skill*5 > severity*100): haunt removed cleanly
///   Partial  (skill*5 > severity*50):  haunt removed + spiritual trauma thought
///   Failure  (otherwise):              haunt severity +0.3, pawn gets brief mental break
///
/// On successful removal, a severity-dependent retaliation may fire.
/// </summary>
public class Recipe_Exorcism : RecipeWorker
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if (!base.AvailableOnNow(thing, part))
            return false;
        if (thing is not Pawn pawn)
            return false;
        return FindTargetHaunt(pawn) != null;
    }

    public override void ApplyOnPawn(
        Pawn pawn,
        BodyPartRecord part,
        Pawn billDoer,
        List<Thing> ingredients,
        Bill bill
    )
    {
        Hediff hauntHediff = FindTargetHaunt(pawn);
        if (hauntHediff == null)
            return;

        float severity = hauntHediff.Severity;
        int medicineSkill = billDoer?.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0;

        if (medicineSkill * 5 > severity * 100)
            ApplySuccess(pawn, billDoer, hauntHediff, severity);
        else if (medicineSkill * 5 > severity * 50)
            ApplyPartial(pawn, billDoer, hauntHediff, severity);
        else
            ApplyFailure(pawn, billDoer, hauntHediff, severity);
    }

    // ── Outcome handlers ──────────────────────────────────────────────────────

    private static void ApplySuccess(Pawn pawn, Pawn doctor, Hediff haunt, float severity)
    {
        string hauntLabel = haunt.def.label;
        TryRetaliate(pawn, severity);
        pawn.health.RemoveHediff(haunt);

        Messages.Message(
            "MSS_FP_Exorcism_Success_Msg".Translate(pawn.LabelShort, hauntLabel),
            pawn,
            MessageTypeDefOf.PositiveEvent,
            false
        );
    }

    private static void ApplyPartial(Pawn pawn, Pawn doctor, Hediff haunt, float severity)
    {
        string hauntLabel = haunt.def.label;
        TryRetaliate(pawn, severity);
        pawn.health.RemoveHediff(haunt);

        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
            MSSFPDefOf.MSS_FP_Exorcism_SpiritualTrauma
        );

        Messages.Message(
            "MSS_FP_Exorcism_Partial_Msg".Translate(pawn.LabelShort, hauntLabel),
            pawn,
            MessageTypeDefOf.NeutralEvent,
            false
        );
    }

    private static void ApplyFailure(Pawn pawn, Pawn doctor, Hediff haunt, float severity)
    {
        haunt.Severity = severity + Mathf.Min(0.3f, 0.9f - severity);

        pawn.mindState?.mentalStateHandler?.TryStartMentalState(
            MentalStateDefOf.Wander_Sad,
            "MSS_FP_Exorcism_Failure_Reason".Translate(),
            forced: false,
            causedByMood: false
        );

        Messages.Message(
            "MSS_FP_Exorcism_Failure_Msg".Translate(pawn.LabelShort, haunt.def.label),
            pawn,
            MessageTypeDefOf.NegativeEvent,
            false
        );
    }

    // ── Retaliation ───────────────────────────────────────────────────────────

    private static void TryRetaliate(Pawn pawn, float severity)
    {
        if (severity < 0.3f)
            return;

        if (severity < 0.6f)
        {
            // 20% chance: all pawns in the same room get a mood hit
            if (!Rand.Chance(0.2f))
                return;
            ApplyBacklashToRoom(pawn);
        }
        else
        {
            // 40% chance: backlash + poltergeist event + work speed debuff
            if (!Rand.Chance(0.4f))
                return;
            ApplyBacklashToRoom(pawn);
            pawn.health.AddHediff(MSSFPDefOf.MSS_FP_Exorcism_Hangover);
            TryFirePoltergeistEvent(pawn);
        }
    }

    private static void ApplyBacklashToRoom(Pawn pawn)
    {
        Room room = pawn.GetRoom();
        if (room == null)
            return;

        foreach (Pawn occupant in room.ContainedAndAdjacentThings.OfType<Pawn>())
        {
            if (!occupant.RaceProps.Humanlike)
                continue;
            occupant.needs?.mood?.thoughts?.memories?.TryGainMemory(
                MSSFPDefOf.MSS_FP_Exorcism_GhostlyBacklash
            );
        }

        Messages.Message(
            "MSS_FP_Exorcism_Backlash_Msg".Translate(pawn.LabelShort),
            pawn,
            MessageTypeDefOf.NegativeEvent,
            false
        );
    }

    private static void TryFirePoltergeistEvent(Pawn pawn)
    {
        if (!MSSFPMod.settings.EnablePoltergeistEvents)
            return;
        if (pawn.Map == null)
            return;

        HauntEventMapComponent eventComp = pawn.Map.GetComponent<HauntEventMapComponent>();
        eventComp?.ForceRandomEvent(pawn);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the highest-severity bad haunt on the pawn, or null if none.</summary>
    private static Hediff FindTargetHaunt(Pawn pawn)
    {
        Hediff best = null;
        float bestSev = -1f;

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is not HediffWithComps hwc)
                continue;
            foreach (HediffComp comp in hwc.comps)
            {
                if (comp is not HediffComp_Haunt haunt)
                    continue;
                // Only bad haunts can be exorcised — good haunts are beneficial
                if (haunt.Props.isGood)
                    continue;
                if (hediff.Severity > bestSev)
                {
                    best = hediff;
                    bestSev = hediff.Severity;
                }
            }
        }

        return best;
    }
}
