using RimWorld;
using Verse;

namespace MSSFP.Projectiles
{
    public class Projectile_PlaqueBullet : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);

            if (hitThing is Pawn targetPawn && !blockedByShield)
                ApplyPlaqueEffect(targetPawn);
        }

        private void ApplyPlaqueEffect(Pawn target)
        {
            if (Rand.Chance(0.01f))
            {
                ApplyAssClosingEffect(target);
                return;
            }

            if (Rand.Chance(0.5f))
                ApplyToothPain(target);
            else
                TransformToSign(target);
        }

        private void ApplyAssClosingEffect(Pawn target)
        {
            var hediff = DefDatabase<HediffDef>.GetNamed("MSS_MendedButtcrack", false);
            if (hediff != null)
            {
                target.health.AddHediff(hediff);
                Messages.Message(
                    $"{target.NameShortColored} had their ass closed by the plaque gun!",
                    target,
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void ApplyToothPain(Pawn target)
        {
            var hediff = DefDatabase<HediffDef>.GetNamed("MSS_ToothPain", false);
            if (hediff != null)
            {
                target.health.AddHediff(hediff);
                Messages.Message(
                    $"{target.NameShortColored} got tooth pain from the plaque gun!",
                    target,
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        private void TransformToSign(Pawn target)
        {
            var map = target.Map;
            var position = target.Position;

            var signDef = DefDatabase<ThingDef>.GetNamed("MSS_PlaqueSign", false);
            if (signDef == null)
            {
                Log.Error($"MSSFP: Sign definition not found for {target.NameShortColored}");
                return;
            }

            var sign = ThingMaker.MakeThing(signDef);
            sign.SetPositionDirect(position);

            var signComp = sign.TryGetComp<Comps.Comp_PlaqueSign>();
            if (signComp == null)
            {
                Log.Error($"MSSFP: Sign component not found for {target.NameShortColored}");
                return;
            }

            if (!signComp.StorePawn(target))
            {
                Log.Error($"MSSFP: Failed to store pawn data for {target.NameShortColored}");
                return;
            }

            if (!GenPlace.TryPlaceThing(sign, position, map, ThingPlaceMode.Direct))
            {
                Log.Error($"MSSFP: Failed to place sign for {target.NameShortColored}");
                return;
            }
        }
    }
}
