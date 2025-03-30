using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(RitualOutcomeEffectWorker_ChildBirth))]
public static class RitualOutcomeEffectWorker_ChildBirth_Patch
{
    // TODO: Investigate why this leads to unending labor in the Catharsis2 pack
    // [HarmonyPatch(nameof(RitualOutcomeEffectWorker_ChildBirth.Apply))]
    // [HarmonyTranspiler]
    // public static IEnumerable<CodeInstruction> Apply_Transpiler(IEnumerable<CodeInstruction> instructions)
    // {
    //     MethodInfo methodToReplace = AccessTools.Method(typeof(PregnancyUtility), "ApplyBirthOutcome");
    //     MethodInfo replacementMethod = AccessTools.Method(typeof(RitualOutcomeEffectWorker_ChildBirth_Patch), "ApplyBirthOutcome");
    //
    //     foreach (CodeInstruction instruction in instructions)
    //     {
    //         if (instruction.opcode == OpCodes.Call && instruction.operand as MethodInfo == methodToReplace)
    //         {
    //             instruction.operand = replacementMethod;
    //         }
    //
    //         yield return instruction;
    //     }
    // }

    public static Thing ApplyBirthOutcome_NewTemp(
        RitualOutcomePossibility outcome,
        float quality,
        Precept_Ritual ritual,
        List<GeneDef> genes,
        Pawn geneticMother,
        Thing birtherThing,
        Pawn father = null,
        Pawn doctor = null,
        LordJob_Ritual lordJobRitual = null,
        RitualRoleAssignments assignments = null,
        bool preventLetter = false
    )
    {
        return ApplyBirthOutcome(outcome, quality, ritual, genes, geneticMother, birtherThing, father, doctor, lordJobRitual, assignments);
    }

    public static Thing ApplyBirthOutcome(
        RitualOutcomePossibility outcome,
        float quality,
        Precept_Ritual ritual,
        List<GeneDef> genes,
        Pawn geneticMother,
        Thing birtherThing,
        Pawn father,
        Pawn doctor,
        LordJob_Ritual lordJobRitual,
        RitualRoleAssignments assignments
    )
    {
        if (birtherThing is not Pawn birtherPawn || !CompUpgradableBed.AllBeds.ToList().Any(b => b.ParentsForPregnancy(birtherPawn).Any()))
            return PregnancyUtility.ApplyBirthOutcome(outcome, quality, ritual, genes, geneticMother, birtherThing, father, doctor, lordJobRitual, assignments);

        CompUpgradableBed comp = CompUpgradableBed.AllBeds.FirstOrDefault(b => b.ParentsForPregnancy(birtherPawn).Any());
        if (comp == null)
            return PregnancyUtility.ApplyBirthOutcome(outcome, quality, ritual, genes, geneticMother, birtherThing, father, doctor, lordJobRitual, assignments);

        List<Pawn> parents = comp.ParentsForPregnancy(birtherPawn);

        List<Pawn> babies = MakeBabies(birtherPawn, parents, comp).ToList();

        bool babiesAreHealthy = Find.Storyteller.difficulty.babiesAreHealthy;
        int positivityIndex = outcome.positivityIndex;
        bool shouldDieDuringBirth =
            Rand.Chance(PregnancyUtility.ChanceMomDiesDuringBirth(quality)) && (birtherPawn.genes == null || !birtherPawn.genes.HasActiveGene(GeneDefOf.Deathless));

        IntVec3? positionOverride = null;
        IntVec3? slotPos;
        if (birtherPawn.Spawned)
        {
            Building_Bed currentBed = birtherPawn.CurrentBed(out int? sleepingSlot);
            slotPos = sleepingSlot.HasValue ? currentBed?.GetFootSlotPos(sleepingSlot.Value) : null;

            IntVec3 intVec3 = slotPos ?? birtherPawn.PositionHeld;
            positionOverride = CellFinder.RandomClosewalkCellNear(
                intVec3,
                birtherPawn.Map,
                1,
                cell =>
                {
                    if (!(cell != birtherPawn.PositionHeld))
                        return false;
                    Building building = birtherPawn.Map.edificeGrid[cell];
                    if (building == null)
                        return true;
                    bool? isBed = building.def?.IsBed;
                    return !(isBed.GetValueOrDefault() & isBed.HasValue);
                }
            );
            SpawnBirthFilth(birtherPawn, intVec3, ThingDefOf.Filth_AmnioticFluid, 1);
            if (shouldDieDuringBirth)
                SpawnBirthFilth(birtherPawn, intVec3, ThingDefOf.Filth_Blood, 2);
        }

        birtherPawn.health.AddHediff(HediffDefOf.PostpartumExhaustion);
        birtherPawn.health.AddHediff(HediffDefOf.Lactating);

        foreach (Pawn babyPawn in babies)
        {
            bool badOutcome = false;
            babyPawn.relations.AddDirectRelation(PawnRelationDefOf.ParentBirth, birtherPawn);
            foreach (Pawn parent in parents)
            {
                babyPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent);
            }
            if (positivityIndex >= 0 || babiesAreHealthy)
            {
                if (babyPawn.playerSettings != null && geneticMother?.playerSettings != null)
                    babyPawn.playerSettings.AreaRestrictionInPawnCurrentMap = geneticMother.playerSettings.AreaRestrictionInPawnCurrentMap;
                if (positivityIndex == 0)
                    babyPawn.health.AddHediff(HediffDefOf.InfantIllness);
                if (birtherPawn != null)
                {
                    babyPawn.mindState.SetAutofeeder(birtherPawn, AutofeedMode.Urgent);
                }
                bool canPassToWorld = true;

                if (doctor is { Spawned: true })
                {
                    bool? nullable2 = doctor.carryTracker?.TryStartCarry((Thing)babyPawn);
                    bool flag5 = true;
                    if (
                        nullable2.GetValueOrDefault() == flag5 & nullable2.HasValue
                        && birtherPawn != null
                        && doctor.CanReachImmediate((LocalTargetInfo)(Thing)birtherPawn, PathEndMode.Touch)
                    )
                    {
                        Job newJob = JobMaker.MakeJob(JobDefOf.CarryToMomAfterBirth, (LocalTargetInfo)(Thing)babyPawn, (LocalTargetInfo)(Thing)birtherPawn);
                        newJob.count = 1;
                        doctor.jobs.StartJob(newJob, JobCondition.Succeeded, keepCarryingThingOverride: true);
                        canPassToWorld = false;
                    }
                }
                if (canPassToWorld && !PawnUtility.TrySpawnHatchedOrBornPawn(babyPawn, birtherThing, positionOverride))
                    Find.WorldPawns.PassToWorld(babyPawn, PawnDiscardDecideMode.Discard);

                if (positivityIndex != 0)
                {
                    foreach (Pawn parent in parents)
                    {
                        parent?.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.BabyBorn, babyPawn);
                    }
                }
            }
            else
            {
                Hediff culpritHediff = babyPawn.health.AddHediff(HediffDefOf.Stillborn);
                badOutcome = true;
                birtherPawn?.Ideo?.Notify_MemberDied(babyPawn);
                babyPawn.babyNamingDeadline = Find.TickManager.TicksGame + 1;
                Find.BattleLog.Add(new BattleLogEntry_StateTransition((Thing)babyPawn, babyPawn.RaceProps.DeathActionWorker.DeathRules, null, culpritHediff, null));
                if (birtherThing.Spawned)
                {
                    Corpse corpse = babyPawn.Corpse;
                    slotPos = positionOverride;
                    IntVec3 loc = slotPos ?? birtherThing.PositionHeld;
                    Map mapHeld = birtherThing.MapHeld;
                    GenSpawn.Spawn(corpse, loc, mapHeld);
                }
            }

            Find.QuestManager.Notify_PawnBorn(babyPawn, birtherThing, geneticMother, father);

            if (ritual != null)
            {
                DoBabyLetter(badOutcome ? babyPawn.Corpse : babyPawn, birtherPawn, badOutcome, shouldDieDuringBirth, quality, ritual, outcome, lordJobRitual, assignments);
            }
        }

        if (shouldDieDuringBirth)
            birtherPawn.Kill(null, null);

        return babies.FirstOrDefault();
    }

    public static void DoBabyLetter(
        Thing baby,
        Pawn birtherPawn,
        bool badOutcome,
        bool shouldDieDuringBirth,
        float quality,
        Precept_Ritual ritual,
        RitualOutcomePossibility outcome,
        LordJob_Ritual lordJobRitual,
        RitualRoleAssignments assignments
    )
    {
        TaggedString letterText = outcome.description.Formatted(birtherPawn.Named("MOTHER"));

        if (birtherPawn != null && !badOutcome)
        {
            RitualOutcomeEffectWorker_ChildBirth instance = (RitualOutcomeEffectWorker_ChildBirth)RitualOutcomeEffectDefOf.ChildBirth.GetInstance();
            if (lordJobRitual != null)
                letterText += "\n\n" + instance.OutcomeQualityBreakdownDesc(quality, 1f, lordJobRitual);
            else
                letterText += "\n\n" + RitualUtility.QualityBreakdownAbstract(ritual, new TargetInfo(birtherPawn.PositionHeld, birtherPawn.MapHeld, true), assignments);

            letterText += "\n\n" + "BirthRitualHealthyBabyChance".Translate(instance.GetOutcomeChanceAtQuality(lordJobRitual, instance.def.BestOutcome, quality));

            if (shouldDieDuringBirth)
                letterText += "\n\n" + "LetterPartColonistDiedAfterChildbirth".Translate((NamedArgument)(Thing)birtherPawn);
        }
        if (baby is Pawn p && p.genes.HasActiveGene(GeneDefOf.Inbred))
            letterText += "\n\n" + "InbredBabyBorn".Translate();
        letterText += ("\n\n" + "LetterPartTempBabyName".Translate((NamedArgument)baby) + " ");
        letterText = !badOutcome
            ? letterText + "LetterPartLiveBirthNameDeadline".Translate((NamedArgument)60000.ToStringTicksToPeriod())
            : letterText + "LetterPartStillbirthNameDeadline".Translate();
        ChoiceLetter_BabyBirth let = (ChoiceLetter_BabyBirth)
            LetterMaker.MakeLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), ritual.Label.Named("RITUALLABEL")), letterText, LetterDefOf.BabyBirth, baby);
        let.Start();
        Find.LetterStack.ReceiveLetter(let);
    }

    public static IEnumerable<Pawn> MakeBabies(Pawn birtherPawn, List<Pawn> parents, CompUpgradableBed comp)
    {
        if (birtherPawn.RaceProps.Humanlike && !ModsConfig.BiotechActive)
            yield break;

        int litterSize = birtherPawn.RaceProps.litterSizeCurve != null ? Mathf.RoundToInt(Rand.ByCurve(birtherPawn.RaceProps.litterSizeCurve)) : 1;
        if (litterSize < 1)
            litterSize = 1;

        if (litterSize == 1)
        {
            litterSize = Rand.RangeInclusive(1, Math.Max(1, parents.Count));
        }

        PawnGenerationRequest request = new(birtherPawn.kindDef, birtherPawn.Faction, allowDowned: true, developmentalStages: DevelopmentalStage.Newborn);
        for (int index = 0; index < litterSize; ++index)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            comp.Notify_PawnBorn(pawn);
            TaleRecorder.RecordTale(TaleDefOf.GaveBirth, birtherPawn, pawn);

            yield return pawn;
        }
    }

    public static void SpawnBirthFilth(Pawn mother, IntVec3 center, ThingDef filth, int radius)
    {
        int randomInRange = new IntRange(4, 7).RandomInRange;
        for (int index = 0; index < randomInRange; ++index)
            FilthMaker.TryMakeFilth(CellFinder.RandomClosewalkCellNear(center, mother.Map, radius), mother.Map, filth, mother.LabelIndefinite());
    }
}
