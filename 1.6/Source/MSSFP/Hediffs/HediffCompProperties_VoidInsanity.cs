using System.Collections.Generic;
using MSSFP.Utils;
using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_VoidInsanity: HediffCompProperties
{
    public float ChanceForMentalBreakPerDay = 0.05f;
    public List<MentalBreakChance> MentalStates;

    public float ChanceForRandomThought = 0.15f;
    public FloatRange ThoughtDaysLength = new(0.5f, 2f);
    public List<ThoughtChance> RandomThoughts;

    public float ChanceForRandomHediff = 0.15f;
    public FloatRange HediffDaysLength = new(0.5f, 2f);
    public List<HediffChance> RandomHediffs;

    public HediffCompProperties_VoidInsanity()
    {
        compClass = typeof(HediffComp_VoidInsanity);
    }
}
