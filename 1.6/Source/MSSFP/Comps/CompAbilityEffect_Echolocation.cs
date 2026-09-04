using MSSFP.Comps.Map;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompAbilityEffect_Echolocation : CompAbilityEffect
{
    public new CompProperties_AbilityEcholocation Props =>
        (CompProperties_AbilityEcholocation)props;

    public override bool GizmoDisabled(out string reason)
    {
        if (parent.pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight) <= 0f)
        {
            reason = null;
            return false;
        }

        reason = $"{parent.pawn.Name.ToStringShort} is not blind.";
        return true;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = parent.pawn;
        caster.Map.GetComponent<EcholocationMapComponent>().StartEffect(caster, Props.durationTicks);
        // combat/pathing patches (T4/T5), buff (T9) land in later tasks.
    }
}
