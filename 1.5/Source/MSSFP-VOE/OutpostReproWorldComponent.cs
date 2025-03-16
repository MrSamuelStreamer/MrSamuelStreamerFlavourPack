using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.VOE;

public class OutpostReproWorldComponent(World world) : WorldComponent(world)
{
    public bool Enabled => MSSFPMod.settings.EnableOutpostFission;
    public int TicksToReproduce => Find.TickManager.TicksGame + MSSFPMod.settings.DaysForOutpostFission * GenDate.TicksPerDay;

    public class AsexualReproducer : IExposable
    {
        public Pawn pawn;
        public int duplicateAfterTick;
        public Outpost outpost;

        public AsexualReproducer() { }

        public AsexualReproducer(Pawn pawn, Outpost outpost, int duplicateAfterTick)
        {
            this.pawn = pawn;
            this.outpost = outpost;
            this.duplicateAfterTick = duplicateAfterTick;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref outpost, "outpost");
            Scribe_Values.Look(ref duplicateAfterTick, "duplicateAfterTick");
        }
    }

    public List<AsexualReproducer> AsexualReproducersInOutposts = new List<AsexualReproducer>();

    public virtual void Notify_PawnAdded(Outpost outpost, Pawn pawn)
    {
        Notify_PawnRemoved(pawn);
        AsexualReproducersInOutposts.Add(new AsexualReproducer(pawn, outpost, TicksToReproduce));
        ;
    }

    public virtual void Notify_PawnRemoved(Pawn pawn)
    {
        AsexualReproducersInOutposts.RemoveWhere(ar => ar.pawn == pawn);
    }

    public virtual void Notify_OutpostRemoved(Outpost outpost)
    {
        AsexualReproducersInOutposts.RemoveWhere(ar => ar.outpost == outpost);
    }

    public override void WorldComponentTick()
    {
        if (!Enabled)
            return;
        if (Find.TickManager.TicksGame % GenDate.TicksPerHour != 0)
            return;

        AsexualReproducersInOutposts.RemoveWhere(ar => !ar.outpost?.AllPawns.Contains(ar.pawn) ?? true);

        foreach (AsexualReproducer asexualReproducer in AsexualReproducersInOutposts)
        {
            if (!asexualReproducer.pawn.ageTracker.Adult)
                continue;
            if (asexualReproducer.duplicateAfterTick < Find.TickManager.TicksGame)
            {
                asexualReproducer.duplicateAfterTick = TicksToReproduce;
                SpawnDuplicateAtOutpost(asexualReproducer.outpost, asexualReproducer.pawn);
            }
        }
    }

    public virtual void SpawnDuplicateAtOutpost(Outpost outpost, Pawn progenitor)
    {
        int num = progenitor.RaceProps.litterSizeCurve == null ? 1 : Mathf.RoundToInt(Rand.ByCurve(progenitor.RaceProps.litterSizeCurve));
        if (num < 1)
        {
            num = 1;
        }

        if (num == 1 && Rand.Chance(0.05f))
            num = (new IntRange(1, 3)).RandomInRange;

        PawnGenerationRequest request = new PawnGenerationRequest(
            progenitor.kindDef,
            progenitor.Faction,
            PawnGenerationContext.NonPlayer,
            -1,
            forceGenerateNewPawn: false,
            allowDead: false,
            allowDowned: true,
            canGeneratePawnRelations: true,
            mustBeCapableOfViolence: false,
            1f,
            forceAddFreeWarmLayerIfNeeded: false,
            allowGay: true,
            allowPregnant: false,
            allowFood: true,
            allowAddictions: true,
            inhabitant: false,
            certainlyBeenInCryptosleep: false,
            forceRedressWorldPawnIfFormerColonist: false,
            worldPawnFactionDoesntMatter: false,
            0f,
            0f,
            null,
            1f,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            forceNoIdeo: false,
            forceNoBackstory: false,
            forbidAnyTitle: false,
            forceDead: false,
            null,
            null,
            null,
            null,
            null,
            0f,
            DevelopmentalStage.Newborn
        );

        Pawn pawnCreated = null;

        for (int i = 0; i < num; i++)
        {
            pawnCreated = PawnGenerator.GeneratePawn(request);

            List<Gene> listOfEndogenes = progenitor.genes!.Endogenes;

            foreach (Gene gene in listOfEndogenes)
            {
                pawnCreated.genes?.AddGene(gene.def, false);
            }

            if (progenitor.genes?.Xenotype != null)
            {
                pawnCreated.genes?.SetXenotype(progenitor.genes?.Xenotype);
            }

            GeneDef repro = DefDatabase<GeneDef>.AllDefsListForReading.FirstOrDefault(g => g.defName == "AG_AsexualFission");

            if (repro != null && progenitor.genes!.HasActiveGene(repro) && Rand.Chance(0.1f))
            {
                pawnCreated.genes!.AddGene(repro, true);
            }

            if (pawnCreated.RaceProps.IsFlesh)
            {
                pawnCreated.relations.AddDirectRelation(PawnRelationDefOf.Parent, progenitor);
            }

            TaleRecorder.RecordTale(TaleDefOf.GaveBirth, progenitor, pawnCreated);

            outpost.AddPawn(pawnCreated);
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref AsexualReproducersInOutposts, "AsexualReproducersInOutposts", LookMode.Deep);
    }
}
