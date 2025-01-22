using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VEE;
using Verse;

namespace MSSFP.VEE;

public class NuclearFallout: GameCondition
{
    public SkyColorSet FalloutRainColors = new(
        new ColorInt(20, 91, 17).ToColor,
        new ColorInt(7, 54, 5).ToColor,
        new ColorInt(25, 149, 21).ToColor,
        0.85f);
    public readonly List<SkyOverlay> overlays = [new WeatherOverlay_Rain()];

    public override bool AllowEnjoyableOutsideNow(Map map) => false;

    public EventPropsDefModExtension Props => def.GetModExtension<EventPropsDefModExtension>();

    public override WeatherDef ForcedWeather()
    {
        Map currentMap = Find.CurrentMap;
        int num1;
        if (currentMap == null)
        {
            num1 = 0;
        }
        else
        {
            float? outdoorTemp = currentMap.mapTemperature?.OutdoorTemp;
            float num2 = 0.0f;
            num1 = outdoorTemp.GetValueOrDefault() <= (double) num2 & outdoorTemp.HasValue ? 1 : 0;
        }
        return num1 != 0 ? VEE_DefOf.SnowHard : VEE_DefOf.Rain;
    }

    public override void Init()
    {
        LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
        LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Critical);
    }

    public override void GameConditionTick()
    {
        foreach (Map affectedMap in AffectedMaps)
        {
            if (Find.TickManager.TicksGame % Props.FalloutCheckTicks == 0)
                DoFallout(affectedMap);
            foreach (SkyOverlay t in overlays)
                t.TickOverlay(affectedMap);
        }
    }

    public virtual void DoFallout(Map map)
    {
        IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
        foreach (Pawn thing in allPawnsSpawned)
        {
            if (!thing.Position.Roofed(map) && thing.def.race.IsFlesh && thing.genes != null)
            {
                if (Rand.Chance(Props.ChanceForNewGenesEachCheck))
                {
                    // random genes
                    bool addAsXeno = Rand.Chance(0.95f);
                    foreach (GeneDef geneDef in RandomGeneSet(thing))
                    {
                        thing.genes.AddGene(geneDef,addAsXeno);
                    }
                }

                if (Rand.Chance(Props.ChanceForRemoveGeneEachCheck))
                {
                    foreach (Gene gene in thing.genes.Xenogenes.Take(Props.GenesToAdd.RandomInRange))
                    {
                        thing.genes.RemoveGene(gene);
                    }
                }
            }
        }
    }

    public virtual List<GeneDef> RandomGeneSet(Pawn pawn)
    {
        int tries = 10;
        while (tries-- > 0)
        {
            List<GeneDef> pawnGenes = pawn.genes.Endogenes.Select(g => g.def).Concat(pawn.genes.Xenogenes.Select(g => g.def)).ToList();

            IEnumerable<GeneDef> validGenePool = DefDatabase<GeneDef>.AllDefs.Except(pawnGenes);

            List<GeneDef> selected = validGenePool.Take(Props.GenesToAdd.RandomInRange).ToList();

            if(selected.Count <= 0) continue;

            List<GeneDef> prereqs = selected.Where(g => g.prerequisite != null).Select(g => g.prerequisite).ToList();

            while (prereqs.Count > 0)
            {
                selected = selected.Concat(prereqs).ToList();
                prereqs = selected.Where(g => g.prerequisite != null).Select(g => g.prerequisite).Except(prereqs).ToList();
            }

            List<GeneDef> conflicts = new List<GeneDef>();

            foreach (GeneDef gene in selected)
            {
                if (!conflicts.Contains(gene))
                {
                    foreach (GeneDef otherGene in pawnGenes.Concat(conflicts).Except(gene).Except(conflicts))
                    {
                        if (gene.ConflictsWith(otherGene))
                        {
                            conflicts.Add(otherGene);
                        }
                    }
                }
            }

            selected = selected.Except(conflicts).ToList();
            if(selected.Count <= 0) continue;

            return selected;
        }

        ModLog.Warn("Tried 10 times to generate a list of random genes and failed");

        return [];
    }

    public override void GameConditionDraw(Map map)
    {
        foreach (SkyOverlay t in overlays)
            t.DrawOverlay(map);
    }

    public override float SkyTargetLerpFactor(Map map)
    {
        return GameConditionUtility.LerpInOutValue(this, 5000f, 0.5f);
    }

    public override SkyTarget? SkyTarget(Map map)
    {
        return new SkyTarget(0.85f, this.FalloutRainColors, 1f, 1f);
    }

    public override List<SkyOverlay> SkyOverlays(Map map) => overlays;
}
