using System.Text;
using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_RandomMessageAfterTicks : HediffComp
{
    private int ticksUntilMessage;

    protected HediffCompProperties_RandomMessageAfterTicks Props
    {
        get => (HediffCompProperties_RandomMessageAfterTicks) props;
    }

    public override void CompPostMake()
    {
        base.CompPostMake();
        ticksUntilMessage = Props.ticks;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (ticksUntilMessage == 0)
        {
            if (PawnUtility.ShouldSendNotificationAbout(Pawn))
            {
                if (Props.letterType != null)
                    Find.LetterStack.ReceiveLetter(Props.letterLabels.RandomElement().Formatted((NamedArgument) (Thing) Pawn), GetLetterText(), Props.letterType, (Thing) Pawn);
            }
        }
        else
        {
            if (ticksUntilMessage <= 0)
                return;
        }

        --ticksUntilMessage;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref ticksUntilMessage, "ticksUntilMessage");
    }

    private TaggedString GetLetterText()
    {
        StringBuilder text = new StringBuilder(Props.letterTexts.RandomElement().Formatted((NamedArgument) (Thing) Pawn));

        if (parent is not Hediff_Pregnant p || p.Mother == null || p.Mother == p.pawn)
            return text.ToString();
        text.AppendLine("IvfPregnancyLetterText".Translate(parent.pawn.NameFullColored));

        if (p.Mother != null && p.Father != null)
            text.AppendLine("");
        text.AppendLine("IvfPregnancyLetterParents".Translate(p.Mother.NameFullColored, p.Father.NameFullColored));
        return text.ToString();
    }
}
