using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilityAssBeckon : CompAbilityEffect
{
    public CompProperties_AbilityAssBeckon Props => (CompProperties_AbilityAssBeckon)props;

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (target.Pawn == null || !target.Pawn.DevelopmentalStage.Adult() || target.Pawn.Inhumanized())
            return false;
        if (!parent.pawn.DevelopmentalStage.Adult() || parent.pawn.Inhumanized())
            return false;

        return !LovePartnerRelationUtility.LovePartnerRelationExists(target.Pawn, parent.pawn) && base.CanApplyOn(target, dest);
    }

    private void BreakLoverAndFianceRelations(Pawn pawn, out List<Pawn> oldLoversAndFiances)
    {
        oldLoversAndFiances = [];
        int num = 200;
        while (num > 0 && !new HistoryEvent(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
        {
            Pawn otherPawn1 = LovePartnerRelationUtility.ExistingLeastLikedPawnWithRelation(pawn, r => r.def == PawnRelationDefOf.Lover);
            if (otherPawn1 != null)
            {
                pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, otherPawn1);
                pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, otherPawn1);
                oldLoversAndFiances.Add(otherPawn1);
            }
            else
            {
                Pawn otherPawn2 = LovePartnerRelationUtility.ExistingLeastLikedPawnWithRelation(pawn, r => r.def == PawnRelationDefOf.Fiance);
                if (otherPawn2 == null)
                    break;
                pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Fiance, otherPawn2);
                pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, otherPawn2);
                oldLoversAndFiances.Add(otherPawn2);
            }

            --num;
        }
    }

    private void RemoveBrokeUpAndFailedRomanceThoughts(Pawn pawn, Pawn otherPawn)
    {
        if (pawn.needs.mood == null)
            return;
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.BrokeUpWithMe, otherPawn);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMe, otherPawn);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMeLowOpinionMood, otherPawn);
    }

    private void TryAddCheaterThought(Pawn pawn, Pawn cheater)
    {
        if (pawn.Dead || pawn.needs.mood == null)
            return;
        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.CheatedOnMe, cheater);
    }

    private void GetNewLoversLetter(
        Pawn initiator,
        Pawn recipient,
        List<Pawn> initiatorOldLoversAndFiances,
        List<Pawn> recipientOldLoversAndFiances,
        bool createdBond,
        out string letterText,
        out string letterLabel,
        out LetterDef letterDef,
        out LookTargets lookTargets
    )
    {
        bool flag = false;
        HistoryEvent ev1 = new(initiator.GetHistoryEventLoveRelationCount(), initiator.Named(HistoryEventArgsNames.Doer));
        HistoryEvent ev2 = new(recipient.GetHistoryEventLoveRelationCount(), recipient.Named(HistoryEventArgsNames.Doer));
        if (!ev1.DoerWillingToDo() || !ev2.DoerWillingToDo())
        {
            letterLabel = "LetterLabelAffair".Translate();
            letterDef = LetterDefOf.NegativeEvent;
            flag = true;
        }
        else
        {
            letterLabel = "LetterLabelNewLovers".Translate();
            letterDef = LetterDefOf.PositiveEvent;
        }
        StringBuilder sb = new StringBuilder();
        if (BedUtility.WillingToShareBed(initiator, recipient))
            sb.AppendLineTagged("LetterNewLovers".Translate(initiator.Named("PAWN1"), recipient.Named("PAWN2")));
        if (flag)
        {
            Pawn firstSpouse1 = initiator.GetFirstSpouse();
            if (firstSpouse1 != null)
            {
                sb.AppendLine();
                sb.AppendLineTagged(
                    "LetterAffair".Translate(
                        (NamedArgument)initiator.LabelShort,
                        (NamedArgument)firstSpouse1.LabelShort,
                        (NamedArgument)recipient.LabelShort,
                        initiator.Named("PAWN1"),
                        recipient.Named("PAWN2"),
                        firstSpouse1.Named("SPOUSE")
                    )
                );
            }
            Pawn firstSpouse2 = recipient.GetFirstSpouse();
            if (firstSpouse2 != null)
            {
                sb.AppendLine();
                sb.AppendLineTagged(
                    "LetterAffair".Translate(
                        (NamedArgument)recipient.LabelShort,
                        (NamedArgument)firstSpouse2.LabelShort,
                        (NamedArgument)initiator.LabelShort,
                        recipient.Named("PAWN1"),
                        firstSpouse2.Named("SPOUSE"),
                        initiator.Named("PAWN2")
                    )
                );
            }
        }
        foreach (Pawn pawn in initiatorOldLoversAndFiances)
        {
            if (pawn.Dead)
                continue;

            sb.AppendLine();
            sb.AppendLineTagged(
                "LetterNoLongerLovers".Translate((NamedArgument)initiator.LabelShort, (NamedArgument)pawn.LabelShort, initiator.Named("PAWN1"), pawn.Named("PAWN2"))
            );
        }
        foreach (Pawn pawn in recipientOldLoversAndFiances)
        {
            if (pawn.Dead)
                continue;
            sb.AppendLine();
            sb.AppendLineTagged(
                "LetterNoLongerLovers".Translate((NamedArgument)recipient.LabelShort, (NamedArgument)pawn.LabelShort, recipient.Named("PAWN1"), pawn.Named("PAWN2"))
            );
        }
        if (createdBond)
        {
            Pawn pawn1 = initiator.genes.GetFirstGeneOfType<Gene_PsychicBonding>() != null ? initiator : recipient;
            Pawn pawn2 = pawn1 == initiator ? recipient : initiator;
            sb.AppendLine();
            sb.AppendLineTagged("LetterPsychicBondCreated".Translate(pawn1.Named("BONDPAWN"), pawn2.Named("OTHERPAWN")));
        }
        letterText = sb.ToString().TrimEndNewlines();
        lookTargets = new LookTargets((TargetInfo)(Thing)initiator, (TargetInfo)(Thing)recipient);
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Pawn initiator = parent.pawn;
        Pawn recipient = target.Pawn;

        BreakLoverAndFianceRelations(initiator, out List<Pawn> initiatorOldLoversAndFiances);
        BreakLoverAndFianceRelations(recipient, out List<Pawn> recipientOldLoversAndFiances);
        RemoveBrokeUpAndFailedRomanceThoughts(initiator, recipient);
        RemoveBrokeUpAndFailedRomanceThoughts(recipient, initiator);

        foreach (Pawn t in initiatorOldLoversAndFiances)
            TryAddCheaterThought(t, initiator);

        foreach (Pawn t in recipientOldLoversAndFiances)
            TryAddCheaterThought(t, recipient);

        initiator.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, recipient);
        initiator.relations.AddDirectRelation(PawnRelationDefOf.Lover, recipient);
        TaleRecorder.RecordTale(TaleDefOf.BecameLover, initiator, recipient);
        bool createdBond = false;
        if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(initiator, recipient))
            createdBond = InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(initiator, recipient);

        string letterText = null;
        string letterLabel = null;
        LetterDef letterDef = null;
        LookTargets lookTargets = null;

        if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
        {
            GetNewLoversLetter(
                initiator,
                recipient,
                initiatorOldLoversAndFiances,
                recipientOldLoversAndFiances,
                createdBond,
                out letterText,
                out letterLabel,
                out letterDef,
                out lookTargets
            );
        }

        LovePartnerRelationUtility.TryToShareBed(initiator, recipient);

        MoteMaker.MakeInteractionBubble(
            initiator,
            recipient,
            InteractionDefOf.RomanceAttempt.interactionMote,
            InteractionDefOf.RomanceAttempt.GetSymbol(initiator.Faction, initiator.Ideo),
            InteractionDefOf.RomanceAttempt.GetSymbolColor(initiator.Faction)
        );

        if (letterDef != null)
        {
            Find.LetterStack.ReceiveLetter((TaggedString)letterLabel, (TaggedString)letterText, letterDef, lookTargets ?? recipient);
        }
    }
}
