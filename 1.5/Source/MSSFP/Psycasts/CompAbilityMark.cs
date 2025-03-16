using System;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilityMark : CompAbilityEffect
{
    public new CompProperties_AbilityMark Props => (CompProperties_AbilityMark)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        PawnGenerationRequest req = new(PawnKindDefOf.SpaceRefugee, allowDead: false, allowDowned: false, fixedGender: Gender.Male, fixedBirthName: "Mark")
        {
            IsCreepJoiner = false,
        };

        Pawn mark = PawnGenerator.GeneratePawn(req);
        mark.Name = new NameTriple("Mark", "Mark", mark.Name is NameTriple triple ? triple.Last : "Marks");
        mark.guest.Recruitable = false;
        mark.needs.mood.thoughts.memories.TryGainMemory(MSSFPDefOf.MSSFP_Marked);
        mark.mindState.mentalBreaker.MentalBreakerTick();
        mark.mindState.mentalStateHandler.TryStartMentalState(MentalBreakDefOf.Berserk.mentalState, "Mark did not appreciate being summoned", forced: true);
        GenSpawn.Spawn(mark, target.Cell, parent.pawn.Map);
    }
}
