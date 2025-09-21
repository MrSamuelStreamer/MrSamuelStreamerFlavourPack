using RimWorld;
using Verse;

namespace MSSFP.Projectiles
{
    public class Projectile_TranquilizerDart : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);

            if (hitThing is Pawn targetPawn && !blockedByShield)
            {
                var damageInfo = new DamageInfo(
                    DamageDef,
                    DamageAmount,
                    ArmorPenetration,
                    ExactRotation.eulerAngles.y,
                    launcher,
                    intendedTarget: intendedTarget.Thing
                );

                var damageResult = targetPawn.TakeDamage(damageInfo);

                if (damageResult.totalDamageDealt > 0 && CausedBleeding(damageResult, targetPawn))
                    ApplyTranquilizerEffect(targetPawn);

                targetPawn.stances?.stagger.StaggerFor(95);
            }
        }

        private bool CausedBleeding(DamageWorker.DamageResult damageResult, Pawn targetPawn)
        {
            // Only works on flesh creatures (not mechanoids)
            if (!targetPawn.RaceProps.IsFlesh || targetPawn.RaceProps.IsMechanoid)
                return false;

            foreach (var hediff in damageResult.hediffs)
            {
                if (hediff is Hediff_Injury injury && injury.Bleeding)
                    return true;
            }

            return false;
        }

        private void ApplyTranquilizerEffect(Pawn target)
        {
            var tranquilizerHediff = DefDatabase<HediffDef>.GetNamed("MSS_Tranquilized");

            if (target.health.hediffSet.HasHediff(tranquilizerHediff))
            {
                var existingHediff = target.health.hediffSet.GetFirstHediffOfDef(
                    tranquilizerHediff
                );
                if (existingHediff != null)
                {
                    var additionalSeverity = 0.1f + Rand.Range(0.0f, 0.1f);
                    existingHediff.Severity = UnityEngine.Mathf.Min(
                        1.0f,
                        existingHediff.Severity + additionalSeverity
                    );
                }
                return;
            }

            var newHediff = target.health.AddHediff(tranquilizerHediff);
            newHediff.Severity = 0.1f + Rand.Range(0.0f, 0.2f);
        }
    }
}
