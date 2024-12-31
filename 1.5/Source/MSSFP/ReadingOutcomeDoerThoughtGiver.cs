using RimWorld;
using Verse;

namespace MSSFP;

public class ReadingOutcomeDoerThoughtGiver: BookOutcomeDoer
{
    public BookOutcomeProperties_ThoughtGiver OutcomeProps => (BookOutcomeProperties_ThoughtGiver)Props;

    public override bool DoesProvidesOutcome(Pawn reader) => true;

    public override void OnReadingTick(Pawn reader, float factor)
    {
        reader.needs?.mood?.thoughts.memories.TryGainMemory(OutcomeProps.ThoughtDef);
    }

}
