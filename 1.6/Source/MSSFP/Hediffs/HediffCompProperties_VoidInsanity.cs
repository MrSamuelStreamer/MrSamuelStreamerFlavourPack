using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_VoidInsanity: HediffCompProperties
{
    public float ChanceForMentalBreakPerDay = 0.05f;
    public List<MentalBreakDef> MentalStates;

    public float ChanceForRandomThought = 0.15f;
    public FloatRange ThoughtDaysLength = new FloatRange(0.5f, 2f);
    public List<ThoughtDef> RandomThoughts;

    public HediffCompProperties_VoidInsanity()
    {
        compClass = typeof(HediffComp_VoidInsanity);
    }
}
