using System;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace MSSFP.Questing;

public class QuestNode_Root_TrekPodCrash : QuestNode_Root_WandererJoin
{
    protected override bool TestRunInt(Slate slate)
    {
        return MSSFPMod.settings.EnableTrekBeamers && base.TestRunInt(slate);
    }

    public override Pawn GeneratePawn()
    {
        Pawn pawn = ThingUtility.FindPawn(MSSFPDefOf.MSSFP_TrekCharacter.root.Generate());
        if (pawn != null)
            pawn.guest.Recruitable = true;
        return pawn;
    }

    [Obsolete]
    public override void SendLetter(Quest quest, Pawn pawn)
    {
        TaggedString title = GetLetterTitle();
        TaggedString taggedString = GetLetterText() + "\n\n";
        TaggedString text =
            taggedString
            + "MSSFP_TrekPodCrash_Factionless".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);

        QuestNode_Root_WandererJoin_WalkIn.AppendCharityInfoToLetter(
            "JoinerCharityInfo".Translate((NamedArgument)(Thing)pawn),
            ref text
        );
        PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
        Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent, new TargetInfo(pawn));
    }

    public virtual string GetLetterTitle()
    {
        GrammarRequest request = new();
        request.Rules.AddRange(MSSFPDefOf.MSS_TrekPodCrash.questNameRules.Rules);
        return NameGenerator.GenerateName(request, rootKeyword: "questName");
    }

    public virtual string GetLetterText()
    {
        return QuestGenUtility.ResolveLocalTextWithDescriptionRules(
            MSSFPDefOf.MSS_TrekPodCrash.questDescriptionRules,
            "questDescription"
        );
    }
}
