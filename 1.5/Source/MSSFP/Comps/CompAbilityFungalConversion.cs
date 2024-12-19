using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompAbilityFungalConversion : CompAbilityEffect
{

        private System.Random rand = new System.Random();



		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{

            base.Apply(target, dest);
            FleckMaker.Static(target.Cell, this.parent.pawn.Map, MSSFPDefOf.PsycastPsychicEffect);

            foreach (Thing current in target.Cell.GetThingList(this.parent.pawn.Map))
            {
                if (current is not Plant plantTarget) continue;

                    PlantProperties plant = plantTarget.def.plant;
                    bool flag = (plant != null);
                    if (flag)
                    {
                        if (plant.IsTree && plantTarget.def.defName == "Plant_TreeGauranlen")
                        {
                            Plant thing2 = (Plant)GenSpawn.Spawn(MSSFPDefOf.MSSFP_Plant_TreeFroganlen, plantTarget.Position, plantTarget.Map, WipeMode.Vanish);
                            Plant thingToDestroy = (Plant)plantTarget;
                            thing2.Growth = thingToDestroy.Growth;
                            plantTarget.Destroy();
                        }
                    }
            }
        }
}
