using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP
{
    public class Recipe_DonateBloodColonist : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part))
                return false;

            if (thing is not Pawn pawn)
                return false;

            if (!pawn.IsFreeColonist || pawn.Downed)
                return false;

            float bloodLoss =
                pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss)?.Severity ?? 0f;
            return bloodLoss < 0.2f;
        }

        public override void ApplyOnPawn(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill
        )
        {
            if (billDoer == null)
                return;

            var hemogenPack = ThingMaker.MakeThing(ThingDefOf.HemogenPack);
            hemogenPack.stackCount = 1;

            var bloodLossHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bloodLossHediff == null)
            {
                bloodLossHediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
                bloodLossHediff.Severity = 0.3f;
                pawn.health.AddHediff(bloodLossHediff);
            }
            else
            {
                bloodLossHediff.Severity += 0.3f;
            }

            GenPlace.TryPlaceThing(
                hemogenPack,
                billDoer.Position,
                billDoer.Map,
                ThingPlaceMode.Near
            );
        }
    }
}
