using System;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilitySummonPawn : CompAbilityEffect
{
    public new CompProperties_AbilitySummonPawn Props => (CompProperties_AbilitySummonPawn)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        for (int i = 0; i < Props.count; i++)
        {
            PawnGenerationRequest req = new(
                Props.RandomPawnKind,
                forceGenerateNewPawn: Props.forceGenerateNewPawn,
                allowDead: Props.allowDead,
                allowDowned: Props.allowDowned,
                colonistRelationChanceFactor: Props.colonistRelationChanceFactor,
                forcedTraits: Props.forcedTraits,
                fixedGender: Props.fixedGender,
                fixedBirthName: Props.fixedBirthName,
                fixedLastName: Props.fixedLastName
            )
            {
                IsCreepJoiner = false,
            };

            Pawn pawn = PawnGenerator.GeneratePawn(req);
            if (!string.IsNullOrEmpty(Props.fixedBirthName))
            {
                string lastname = null;

                if (pawn.Name is NameTriple triple)
                {
                    lastname = triple.Last;
                }

                if (!string.IsNullOrEmpty(Props.fixedLastName))
                {
                    lastname = Props.fixedLastName;
                }

                pawn.Name = new NameTriple(Props.fixedBirthName, Props.fixedBirthName, lastname);
            }
            pawn.guest.Recruitable = Props.recruitable;

            if (Props.thoughtOnSummon != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(Props.thoughtOnSummon);
            }
            if (Props.mentalState != null)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    Props.mentalState,
                    Props.mentalStateReason.Translate(pawn.Name.ToStringShort),
                    forced: true
                );
            }

            GenSpawn.Spawn(pawn, target.Cell, parent.pawn.Map);
        }
    }
}
