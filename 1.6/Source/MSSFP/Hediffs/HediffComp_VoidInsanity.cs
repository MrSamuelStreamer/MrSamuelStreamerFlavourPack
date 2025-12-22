using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_VoidInsanity : HediffComp
{
    public HediffCompProperties_VoidInsanity Props => (HediffCompProperties_VoidInsanity) props;

    public float ChanceMultiplier => parent.Severity;

    public static SortedDictionary<float, List<MentalBreakIntensity>> AllowedIntensities = new()
    {
        {0.0f, [MentalBreakIntensity.Minor] },
        {0.25f, [MentalBreakIntensity.Minor] },
        {0.5f, [MentalBreakIntensity.Minor, MentalBreakIntensity.Major] },
        {0.75f, [MentalBreakIntensity.Minor, MentalBreakIntensity.Major, MentalBreakIntensity.Extreme] },
    };

    public static List<MentalBreakIntensity> AllowedIntensitiesForSeverity(float severity)
    {
        float selectedKey = 0.0f;
        foreach (float key in AllowedIntensities.Keys)
        {
            if (severity >= key)
            {
                selectedKey = key;
            }
            else
            {
                break;
            }
        }
        return AllowedIntensities[selectedKey];
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        float chanceForMentalBreak = (Props.ChanceForMentalBreakPerDay / GenDate.TicksPerDay) * delta * Mathf.Max(0.45f, ChanceMultiplier);
        bool shouldBreak = Rand.Chance(chanceForMentalBreak);
        if (shouldBreak)
        {

            MentalBreakIntensity level = AllowedIntensitiesForSeverity(ChanceMultiplier).RandomElement();

            MentalBreakDef def;
            if (!Props.MentalStates.NullOrEmpty())
            {
                def = Props.MentalStates.RandomElement();
            }
            else
            {
                if (!parent.pawn.mindState.mentalBreaker.TryGetRandomMentalBreak(level, out def))
                {
                    ModLog.Warn("Failed to get a random def");
                }
            }

            if(def != null) parent.pawn.mindState.mentalBreaker.TryDoMentalBreak("MSSFP_VoidBreak".Translate(), def);
        }

        float chanceForRandomThought = (Props.ChanceForRandomThought / GenDate.TicksPerDay) * delta *  Mathf.Max(0.45f, ChanceMultiplier);

        bool shouldAddThought = Rand.Chance(chanceForRandomThought);
        if (shouldAddThought)
        {
            ThoughtDef def = Props.RandomThoughts.RandomElement();
            parent.pawn.needs.mood.thoughts.memories.TryGainMemory(def);
            parent.pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(def).durationTicksOverride = Mathf.FloorToInt(GenDate.TicksPerDay * Props.ThoughtDaysLength.RandomInRange);
        }
    }

}
