using System.Collections.Generic;
using System.Linq;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP;

public class BuffGameCondition: GameCondition
{
    public GameConditionModExtension extension => def.GetModExtension<GameConditionModExtension>();

    public override void Init()
    {
        base.Init();
        hideSource = true;

        if (extension == null)
        {
            ModLog.Error($"MSSFP: GameCondition {def.defName} has no extension");
        }
    }

    public override void GameConditionTick()
    {
        base.GameConditionTick();

        if(extension == null) return;

        if (Find.TickManager.TicksGame % 300 == 0)
        {
            IEnumerable<Pawn> pawns = Find.CurrentMap.mapPawns.AllPawns
                .Where(pawn => pawn.IsAnimal == extension.canTargetAnimals)
                .Where(pawn => pawn.IsColonyMech == extension.canTargetMechs)
                .Where(pawn => pawn.RaceProps.Humanlike == extension.canTargetHumans)
                .Where(pawn => pawn.IsSubhuman == extension.canTargetSubhumans)
                .Where(pawn => pawn.IsEntity == extension.canTargetEntities)
                .Where(pawn => pawn.IsBloodfeeder() == extension.canTargetBloodfeeders);

            if (extension.mustTargetPlayerFaction)
            {
                pawns = pawns.Where(p => p.Faction.IsPlayer);
            }

            List<Pawn> pawnsToBuff = pawns.ToList();
            // ModLog.Log($"MSSFP: Buffing {pawnsToBuff.Count()} pawns with {extension.hediffsToApply.Count()} hediffs");
            foreach (Pawn pawn in pawnsToBuff)
            {
                foreach (HediffDef hediffDef in extension.hediffsToApply)
                {
                    ModLog.Debug($"Applying to {pawn.Name}");
                    pawn.health.GetOrAddHediff(hediffDef);
                }
            }
        }
    }
}
