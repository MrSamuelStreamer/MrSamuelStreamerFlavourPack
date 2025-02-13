using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompFroggoHeal: CompAbilityEffect
{
    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && target.Pawn != null && !target.Pawn.health.Dead && target.Pawn.Faction == Faction.OfPlayer;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        if (ModsConfig.AnomalyActive)
        {
            Effecter effecter = EffecterDefOf.PsychicRitual_Sustained.SpawnAttached(target.Pawn, target.Pawn.Map);
            effecter.Trigger((TargetInfo) (Thing) target.Pawn, (TargetInfo) (Thing) target.Pawn);
            effecter.Cleanup();
        }

        for (int i = 0; i < 100; i++) // emergency breakout
        {
            HealthUtility.TryGetWorstHealthCondition(target.Pawn, out Hediff hediff, out BodyPartRecord part);

            if (hediff is not null)
            {
                if(!hediff.def.isBad) break;
                HealthUtility.Cure(hediff);
            }else if (part is not null)
            {
                target.Pawn.health.RestorePart(part);
            }
            else
            {
                break;
            }
        }
        foreach (Hediff hediff in target.Pawn.health.hediffSet.hediffs.Where(h=>h.Visible))
        {
            HealthUtility.Cure(hediff);
        }
    }
}
