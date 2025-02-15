using System;
using System.Collections.Generic;
using System.Reflection;
using AnimalBehaviours;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace MSSFP.VFE;

public class HediffComp_TweakedAsexualReproduction : HediffComp_AsexualReproduction
{
    // Couldn't get harmony patches to work for what I needed to do, so had to copy/paste. Sorry VFE team
    // Source https://github.com/Vanilla-Expanded/VanillaExpandedFramework/blob/e48ac7b18c095f8f4d585a88ea8089b926f71e94/Source/VFECore/AnimalBehaviours/Hediffs/HediffComp_AsexualReproduction.cs
    public override void CompPostTick(ref float severity)
    {
        Pawn pawn = parent.pawn;
        //Important, without a null map check creatures will reproduce while on caravans, producing errors
        if (pawn.Map != null)
        {
            if (Props.isGreenGoo)
            {
                asexualFissionCounter++;
                //This checks if the map has been filled with creatures. If __instance check wasn't made, the animal would fill
                //the map and grind the game to a halt
                if (asexualFissionCounter >= ticksInday * reproductionIntervalDays &&
                    pawn.Map != null && pawn.Map.listerThings.ThingsOfDef(ThingDef.Named(Props.GreenGooTarget)).Count < Props.GreenGooLimit)
                {
                    //The offspring has the pawn as both mother and father and I find __instance funny
                    Hediff_Pregnant.DoBirthSpawn(pawn, pawn);
                    //Only show a message if the pawn is part of the player's faction
                    if (pawn.Faction == Faction.OfPlayer)
                    {
                        Messages.Message(Props.asexualCloningMessage.Translate(pawn.LabelIndefinite().CapitalizeFirst()), pawn, MessageTypeDefOf.PositiveEvent, true);
                    }

                    asexualFissionCounter = 0;
                }
                //Just reset the counter if the map is filled
                else if (asexualFissionCounter >= ticksInday * reproductionIntervalDays)
                {
                    asexualFissionCounter = 0;
                }
            }

            //Non-green goo creatures only reproduce if they are part of the player's faction, like vanilla animals
            else if (pawn.Faction == Faction.OfPlayer && pawn.ageTracker.CurLifeStage.reproductive)
            {
                asexualFissionCounter++;
                if (asexualFissionCounter >= ticksInday * reproductionIntervalDays)
                {
                    //If it produces eggs or spores, it will just spawn them. Note that these eggs are not part of the player's
                    //faction, the animal hatched from them will be wild
                    if (Props.produceEggs)
                    {
                        GenSpawn.Spawn(ThingDef.Named(Props.eggDef), pawn.Position, pawn.Map);
                        Messages.Message(Props.asexualEggMessage.Translate(pawn.LabelIndefinite().CapitalizeFirst()), pawn, MessageTypeDefOf.PositiveEvent, true);
                        asexualFissionCounter = 0;
                    }
                    //If not, do a normal fission
                    else
                    {
                        if (Props.convertsIntoAnotherDef)
                        {
                            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDef.Named(Props.newDef), pawn.Faction, PawnGenerationContext.NonPlayer, -1, false,
                                true, false, false, true, 1f, false, false, true, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null,
                                null, null, null, null, null, null);
                            Pawn pawnToGenerate = PawnGenerator.GeneratePawn(request);
                            PawnUtility.TrySpawnHatchedOrBornPawn(pawnToGenerate, pawn);
                            Messages.Message(Props.asexualHatchedMessage.Translate(pawn.LabelIndefinite().CapitalizeFirst()), pawn, MessageTypeDefOf.PositiveEvent, true);
                            asexualFissionCounter = 0;
                        }
                        else
                        {
                            Pawn progenitor = parent.pawn;
                            int num = progenitor.RaceProps.litterSizeCurve == null ? 1 : Mathf.RoundToInt(Rand.ByCurve(progenitor.RaceProps.litterSizeCurve));
                            if (num < 1)
                            {
                                num = 1;
                            }

                            PawnGenerationRequest request = new PawnGenerationRequest(progenitor.kindDef, progenitor.Faction, PawnGenerationContext.NonPlayer, -1,
                                forceGenerateNewPawn: false, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f,
                                forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false,
                                certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null,
                                null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false,
                                null, null, null, null, null, 0f, DevelopmentalStage.Newborn);
                            Pawn pawnCreated = null;
                            for (int i = 0; i < num; i++)
                            {
                                pawnCreated = PawnGenerator.GeneratePawn(request);

                                if (Props.endogeneTransfer)
                                {
                                    List<Gene> listOfEndogenes = progenitor.genes?.Endogenes;

                                    foreach (Gene gene in listOfEndogenes)
                                    {
                                        pawnCreated.genes?.AddGene(gene.def, false);
                                    }

                                    if (progenitor.genes?.Xenotype != null)
                                    {
                                        pawnCreated.genes?.SetXenotype(progenitor.genes?.Xenotype);
                                    }

                                    GeneDef repro = DefDatabase<GeneDef>.AllDefsListForReading.FirstOrDefault(g => g.defName == "AG_AsexualFission");

                                    if (repro != null && progenitor.genes.HasActiveGene(repro) && Rand.Chance(0.25f))
                                    {
                                        pawnCreated.genes.AddGene(repro, true);
                                    }
                                }

                                if (PawnUtility.TrySpawnHatchedOrBornPawn(pawnCreated, progenitor))
                                {
                                    if (pawnCreated.playerSettings != null && progenitor.playerSettings != null)
                                    {
                                        pawnCreated.playerSettings.AreaRestrictionInPawnCurrentMap = progenitor.playerSettings.AreaRestrictionInPawnCurrentMap;
                                    }

                                    if (pawnCreated.RaceProps.IsFlesh)
                                    {
                                        pawnCreated.relations.AddDirectRelation(PawnRelationDefOf.Parent, progenitor);
                                    }

                                    if (progenitor.Spawned)
                                    {
                                        progenitor.GetLord()?.AddPawn(pawnCreated);
                                    }
                                }
                                else
                                {
                                    Find.WorldPawns.PassToWorld(pawnCreated, PawnDiscardDecideMode.Discard);
                                }

                                TaleRecorder.RecordTale(TaleDefOf.GaveBirth, progenitor, pawn);
                            }

                            if (progenitor.Spawned)
                            {
                                FilthMaker.TryMakeFilth(progenitor.Position, progenitor.Map, ThingDefOf.Filth_AmnioticFluid, progenitor.LabelIndefinite(), 5);
                                if (progenitor.caller != null)
                                {
                                    progenitor.caller.DoCall();
                                }

                                if (pawn.caller != null)
                                {
                                    pawn.caller.DoCall();
                                }
                            }


                            MSSFPCFEDefOf.MSSFP_Squelch.PlayOneShot(SoundInfo.InMap(Pawn));
                            Messages.Message(Props.asexualHatchedMessage.Translate(pawn.LabelIndefinite().CapitalizeFirst()), pawn, MessageTypeDefOf.PositiveEvent, true);

                            asexualFissionCounter = 0;
                        }
                    }
                }
            }
        }
    }
}
