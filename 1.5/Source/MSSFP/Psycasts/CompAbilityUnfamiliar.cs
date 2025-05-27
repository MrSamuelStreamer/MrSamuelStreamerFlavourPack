using System;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilityUnfamiliar : CompAbilityEffect
{
    public new CompProperties_AbilityUnfamiliar Props => (CompProperties_AbilityUnfamiliar)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        PawnGenerationRequest req = new(PawnKindDefOf.SpaceRefugee, allowDead: false, allowDowned: false) { IsCreepJoiner = false };

        Pawn unfamiliar = PawnGenerator.GeneratePawn(req);
        unfamiliar.guest.Recruitable = false;
        unfamiliar.mindState.mentalBreaker.MentalBreakerTick();
        unfamiliar.mindState.mentalStateHandler.TryStartMentalState(MentalBreakDefOf.Berserk.mentalState, $"{unfamiliar.Name} did not appreciate being summoned", forced: true);
        GenSpawn.Spawn(unfamiliar, target.Cell, parent.pawn.Map);
    }
}
