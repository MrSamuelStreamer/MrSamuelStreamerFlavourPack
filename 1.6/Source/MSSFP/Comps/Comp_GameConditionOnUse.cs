using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class Comp_GameConditionOnUse: ThingComp
{
    public CompProperties_GameConditionOnUse Props => (CompProperties_GameConditionOnUse)props;
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        TargetingParameters parameters = new() {
            canTargetAnimals = Props.canTargetAnimals,
            canTargetHumans = Props.canTargetHumans,
            canTargetSubhumans = Props.canTargetSubhumans,
            canTargetEntities = Props.canTargetEntities,
            canTargetBloodfeeders = Props.canTargetBloodfeeders
        };

        if (Props.mustTargetPlayerFaction)
        {
            parameters.onlyTargetFactions = [Faction.OfPlayer];
        }

        Command_Target useCommand = new() {
            defaultLabel = Props.label,
            defaultDesc = Props.description,
            icon = Props.Icon,
            targetingParams = parameters,
            action = info =>
            {
                GameCondition cond = GameConditionMaker.MakeConditionPermanent(Props.condition);
                Find.World.GameConditionManager.RegisterCondition(cond);

                if (Props.consumeOnUse)
                {
                    parent.Destroy();
                }
            }
        };


        yield return useCommand;
    }

}
