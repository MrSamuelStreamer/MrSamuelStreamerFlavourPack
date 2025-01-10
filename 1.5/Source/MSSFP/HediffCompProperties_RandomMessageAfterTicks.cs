using System.Collections.Generic;
using Verse;

namespace MSSFP;

public class HediffCompProperties_RandomMessageAfterTicks: HediffCompProperties
{
    public int ticks;
    public LetterDef letterType;
    [MustTranslate]
    public List<string> letterTexts;
    [MustTranslate]
    public List<string> letterLabels;

    public HediffCompProperties_RandomMessageAfterTicks()
    {
        compClass = typeof (HediffComp_RandomMessageAfterTicks);
    }

    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (ticks <= 0)
            yield return "ticks must be a positive value";
        if(letterTexts.NullOrEmpty())
            yield return "message list is null or empty";
        if(letterLabels.NullOrEmpty())
            yield return "message list is null or empty";
    }
}
