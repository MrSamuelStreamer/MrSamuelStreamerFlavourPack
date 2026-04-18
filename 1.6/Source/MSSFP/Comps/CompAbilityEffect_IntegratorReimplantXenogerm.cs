using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompAbilityEffect_IntegratorReimplantXenogerm : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (!ModLister.CheckBiotech("xenogerm reimplantation"))
        {
            return;
        }

        base.Apply(target, dest);
        Pawn recipient = target.Pawn;
        if (recipient == null)
        {
            return;
        }

        Pawn caster = parent.pawn;
        GeneUtility.ReimplantXenogerm(caster, recipient);
        FleckMaker.AttachedOverlay(recipient, FleckDefOf.FlashHollow, new Vector3(0f, 0f, 0.26f));
        if (PawnUtility.ShouldSendNotificationAbout(caster) || PawnUtility.ShouldSendNotificationAbout(recipient))
        {
            if (caster.Dead)
            {
                Find.LetterStack.ReceiveLetter(
                    "MSSFP_LetterLabelGenesImplantedCasterDied".Translate(),
                    "MSSFP_LetterTextGenesImplantedCasterDied".Translate(
                        caster.Named("CASTER"),
                        recipient.Named("TARGET")
                    ),
                    LetterDefOf.NegativeEvent,
                    new LookTargets(caster, recipient)
                );
            }
            else
            {
                int comaMax = HediffDefOf
                    .XenogerminationComa.CompProps<HediffCompProperties_Disappears>()
                    .disappearsAfterTicks.max;
                int shockMax = HediffDefOf
                    .XenogermLossShock.CompProps<HediffCompProperties_Disappears>()
                    .disappearsAfterTicks.max;
                Find.LetterStack.ReceiveLetter(
                    "LetterLabelGenesImplanted".Translate(),
                    "LetterTextGenesImplanted".Translate(
                        caster.Named("CASTER"),
                        recipient.Named("TARGET"),
                        comaMax.ToStringTicksToPeriod().Named("COMADURATION"),
                        shockMax.ToStringTicksToPeriod().Named("SHOCKDURATION")
                    ),
                    LetterDefOf.NeutralEvent,
                    new LookTargets(caster, recipient)
                );
            }
        }

        if (caster.Dead || caster.genes == null)
        {
            return;
        }

        caster.genes.SetXenotype(XenotypeDefOf.Baseliner);
        Hediff replicating = caster.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.XenogermReplicating);
        if (replicating != null)
        {
            caster.health.RemoveHediff(replicating);
        }
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        Pawn pawn = target.Pawn;
        if (pawn == null)
        {
            return base.Valid(target, throwMessages);
        }

        if (pawn.IsQuestLodger())
        {
            if (throwMessages)
            {
                Messages.Message(
                    "MessageCannotImplantInTempFactionMembers".Translate(),
                    pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false
                );
            }

            return false;
        }

        if (pawn.HostileTo(parent.pawn) && !pawn.Downed)
        {
            if (throwMessages)
            {
                Messages.Message(
                    "MessageCantUseOnResistingPerson".Translate(parent.def.Named("ABILITY")),
                    pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false
                );
            }

            return false;
        }

        if (parent.pawn.genes?.Xenogenes?.Any() != true)
        {
            if (throwMessages)
            {
                Messages.Message(
                    "MessagePawnHasNoXenogenes".Translate(parent.pawn),
                    parent.pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false
                );
            }

            return false;
        }

        return base.Valid(target, throwMessages);
    }

    public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
    {
        if (GeneUtility.PawnWouldDieFromReimplanting(parent.pawn))
        {
            return Dialog_MessageBox.CreateConfirmation(
                "WarningPawnWillDieFromReimplanting".Translate(parent.pawn.Named("PAWN")),
                confirmAction,
                destructive: true
            );
        }

        return null;
    }

    public override IEnumerable<Mote> CustomWarmupMotes(LocalTargetInfo target)
    {
        Pawn pawn = target.Pawn;
        if (pawn == null)
        {
            yield break;
        }

        yield return MoteMaker.MakeAttachedOverlay(
            pawn,
            ThingDefOf.Mote_XenogermImplantation,
            new Vector3(0f, 0f, 0.3f)
        );
    }
}
