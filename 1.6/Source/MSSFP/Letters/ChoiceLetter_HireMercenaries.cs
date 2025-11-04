using System.Collections.Generic;
using MSSFP.Comps.Map;
using MSSFP.Incidents;
using MSSFP.Pawns;
using RimWorld;
using Verse;

namespace MSSFP.Letters
{
    public class ChoiceLetter_HireMercenaries : ChoiceLetter
    {
        // Contract details
        public Map map;
        public int cost;
        public int contractDuration;
        public string mercenaryName;
        public int timeoutTicks = 60000; // 24 hours

        // Prevent multiple executions
        private bool hasBeenProcessed = false;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                    yield break;
                }

                // Accept option
                DiaOption acceptOption = CreateAcceptOption();
                yield return acceptOption;

                // Reject option
                DiaOption rejectOption = CreateRejectOption();
                yield return rejectOption;

                // Postpone option
                yield return Option_Postpone;
            }
        }

        private DiaOption CreateAcceptOption()
        {
            DiaOption acceptOption = new DiaOption("AcceptButton".Translate());

            acceptOption.action = delegate()
            {
                if (hasBeenProcessed)
                    return;
                hasBeenProcessed = true;

                ProcessHiring();
                Find.LetterStack.RemoveLetter(this);
            };

            acceptOption.resolveTree = true;

            // Disable if not enough silver
            if (!IncidentWorker_HireMercenaries.PlayerHasEnoughSilver(map, cost))
            {
                acceptOption.Disable("NotEnoughSilver".Translate());
            }

            return acceptOption;
        }

        private DiaOption CreateRejectOption()
        {
            DiaOption rejectOption = new DiaOption("RejectLetter".Translate());

            rejectOption.action = delegate()
            {
                if (hasBeenProcessed)
                    return;
                hasBeenProcessed = true;

                Find.LetterStack.RemoveLetter(this);
            };

            rejectOption.resolveTree = true;

            return rejectOption;
        }

        private void ProcessHiring()
        {
            try
            {
                if (IncidentWorker_HireMercenaries.PlayerHasEnoughSilver(map, cost))
                {
                    // Consume the silver
                    IncidentWorker_HireMercenaries.ConsumeSilver(map, cost);

                    // Generate and spawn the mercenary
                    Pawn mercenary = GenerateMercenary();
                    if (mercenary != null)
                    {
                        SetupMercenaryContract(mercenary);
                        SendSuccessNotification(mercenary);
                    }
                    else
                    {
                        SendFailureNotification();
                    }
                }
                else
                {
                    SendFailureNotification();
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error processing hiring: " + ex.Message);
                SendFailureNotification();
            }
        }

        private void SetupMercenaryContract(Pawn mercenary)
        {
            try
            {
                MercenaryContractMapComponent comp =
                    map.GetComponent<MercenaryContractMapComponent>();
                if (comp == null)
                {
                    comp = new MercenaryContractMapComponent(map);
                    map.components.Add(comp);
                }

                comp.StartContract(new List<Pawn> { mercenary }, contractDuration, cost);
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error setting up mercenary contract: " + ex.Message);
            }
        }

        private void SendSuccessNotification(Pawn mercenary)
        {
            try
            {
                string durationText = (contractDuration / GenDate.TicksPerDay).ToString();
                Find.LetterStack.ReceiveLetter(
                    "MSSFP_HireMercenariesAcceptedLabel".Translate(),
                    "MSSFP_HireMercenariesAcceptedText".Translate(
                        mercenary.NameShortColored,
                        cost.ToString(),
                        durationText
                    ),
                    LetterDefOf.PositiveEvent,
                    new LookTargets(mercenary)
                );
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error sending success notification: " + ex.Message);
            }
        }

        private void SendFailureNotification()
        {
            try
            {
                Find.LetterStack.ReceiveLetter(
                    "MSSFP_HireMercenariesFailedLabel".Translate(),
                    "MSSFP_HireMercenariesFailedText".Translate(cost.ToString()),
                    LetterDefOf.NegativeEvent
                );
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error sending failure notification: " + ex.Message);
            }
        }

        private Pawn GenerateMercenary()
        {
            try
            {
                if (map == null)
                {
                    Log.Error("MSSFP: Cannot generate mercenary - map is null");
                    return null;
                }

                // Try to find a suitable spawn location
                if (
                    !CellFinder.TryFindRandomEdgeCellWith(
                        c => map.reachability.CanReachColony(c) && !c.Fogged(map),
                        map,
                        CellFinder.EdgeRoadChance_Ignore,
                        out IntVec3 spawnCell
                    )
                )
                {
                    Log.Warning("MSSFP: Could not find suitable spawn location for mercenary");
                    return null;
                }

                Pawn mercenary = null;

                // If no name provided, generate default combat pawn directly
                if (string.IsNullOrEmpty(mercenaryName))
                {
                    mercenary = GenerateDefaultCombatPawn();
                }
                else
                {
                    // Try to load the specific pawn from PawnEditor XML
                    mercenary = SpecificPawnLoader.GetSpecificPawn(mercenaryName);

                    // If specific pawn loading failed, fall back to default combat pawn
                    if (mercenary == null)
                    {
                        Log.Warning(
                            $"MSSFP: Failed to load specific pawn {mercenaryName}, generating default combat pawn"
                        );
                        mercenary = GenerateDefaultCombatPawn();
                    }
                }

                if (mercenary == null)
                {
                    Log.Error(
                        "MSSFP: Failed to generate mercenary (both specific and default generation failed)"
                    );
                    return null;
                }

                // Set faction only if different and spawn the mercenary
                if (mercenary.Faction != Faction.OfPlayer)
                {
                    mercenary.SetFaction(Faction.OfPlayer);
                }
                GenSpawn.Spawn(mercenary, spawnCell, map);
                mercenary.guest.Notify_PawnRecruited();

                return mercenary;
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error generating mercenary: " + ex.Message);
                return null;
            }
        }

        private Pawn GenerateDefaultCombatPawn()
        {
            try
            {
                // Generate a Man in Black style combat-capable pawn at colony level
                var request = new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    mustBeCapableOfViolence: true,
                    canGeneratePawnRelations: false,
                    colonistRelationChanceFactor: 0f
                );

                Pawn pawn = PawnGenerator.GeneratePawn(request);
                return pawn;
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error generating default combat pawn: " + ex.Message);
                return null;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref cost, "cost");
            Scribe_Values.Look(ref contractDuration, "contractDuration");
            Scribe_Values.Look(ref mercenaryName, "mercenaryName");
            Scribe_Values.Look(ref timeoutTicks, "timeoutTicks", 60000);
            Scribe_Values.Look(ref hasBeenProcessed, "hasBeenProcessed", false);
        }
    }
}
