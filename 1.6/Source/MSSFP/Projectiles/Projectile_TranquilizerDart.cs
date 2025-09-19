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
                ApplyTranquilizerEffect(targetPawn);
        }

        private void ApplyTranquilizerEffect(Pawn target)
        {
            if (target.RaceProps.IsMechanoid)
                return;

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
