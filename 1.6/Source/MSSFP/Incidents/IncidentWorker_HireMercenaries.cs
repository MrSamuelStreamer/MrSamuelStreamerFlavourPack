using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps.Map;
using MSSFP.Letters;
using MSSFP.Pawns;
using RimWorld;
using Verse;

namespace MSSFP.Incidents
{
    public class IncidentWorker_HireMercenaries : IncidentWorker
    {
        // Configuration constants
        public static readonly IntRange ContractDurationRange = new(
            GenDate.TicksPerSeason,
            GenDate.TicksPerSeason * 3
        );
        public static readonly IntRange CostPerMercenaryRange = new(500, 1500);

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!MSSFPMod.settings.EnableMercenaryHiring)
                return false;

            if (parms.target is not Map map)
                return false;

            MercenaryContractMapComponent comp = map.GetComponent<MercenaryContractMapComponent>();
            if (comp?.HasActiveContract == true)
                return false;

            return base.CanFireNowSub(parms);
        }

        public static int GetTotalCost(int mercenaryCount = 1)
        {
            if (mercenaryCount <= 0)
            {
                Log.Warning("MSSFP: Invalid mercenary count: " + mercenaryCount);
                return 0;
            }

            int costPerMercenary = CostPerMercenaryRange.RandomInRange;
            return costPerMercenary * mercenaryCount;
        }

        public static bool PlayerHasEnoughSilver(Map map, int requiredAmount)
        {
            if (map == null || requiredAmount <= 0)
                return false;

            return map.listerThings.AllThings.Where(t =>
                        t.def == ThingDefOf.Silver && !t.IsForbidden(Faction.OfPlayer)
                    )
                    .Sum(t => t.stackCount) >= requiredAmount;
        }

        public static void ConsumeSilver(Map map, int amount)
        {
            if (amount <= 0)
                return;

            if (map == null)
            {
                Log.Error("MSSFP: Cannot consume silver - map is null");
                return;
            }

            int remainingToConsume = amount;
            List<Thing> silverPiles = map
                .listerThings.AllThings.Where(t =>
                    t.def == ThingDefOf.Silver && !t.IsForbidden(Faction.OfPlayer)
                )
                .OrderBy(t => t.stackCount)
                .ToList();

            int totalAvailable = silverPiles.Sum(t => t.stackCount);
            if (totalAvailable < amount)
            {
                Log.Warning(
                    "MSSFP: Not enough silver to consume "
                        + amount
                        + " (available: "
                        + totalAvailable
                        + ")"
                );
                return;
            }

            foreach (Thing silver in silverPiles)
            {
                if (remainingToConsume <= 0)
                    break;

                int toTake = Math.Min(remainingToConsume, silver.stackCount);
                silver.SplitOff(toTake).Destroy(DestroyMode.Vanish);
                remainingToConsume -= toTake;
            }
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!MSSFPMod.settings.EnableMercenaryHiring)
                return false;

            if (parms.target is not Map map)
                return false;

            // Generate offer details
            int totalCost = GetTotalCost();
            int contractDuration = ContractDurationRange.RandomInRange;
            string mercenaryName = GetRandomMercenaryName();

            // Create and send the choice letter
            SendMercenaryOffer(map, totalCost, contractDuration, mercenaryName);

            return true;
        }

        private void SendMercenaryOffer(
            Map map,
            int totalCost,
            int contractDuration,
            string mercenaryName
        )
        {
            try
            {
                // Create choice letter using LetterMaker
                ChoiceLetter_HireMercenaries choiceLetter = (ChoiceLetter_HireMercenaries)
                    LetterMaker.MakeLetter(
                        "MSSFP_HireMercenariesOfferLabel".Translate(),
                        "MSSFP_HireMercenariesOfferText".Translate(
                            mercenaryName,
                            totalCost.ToString(),
                            (contractDuration / GenDate.TicksPerDay).ToString()
                        ),
                        MSSFPDefOf.MSSFP_HireMercenariesOffer,
                        (Faction)null
                    );

                // Configure the letter
                choiceLetter.title = "MSSFP_HireMercenariesOfferTitle".Translate();
                choiceLetter.radioMode = true;
                choiceLetter.map = map;
                choiceLetter.cost = totalCost;
                choiceLetter.contractDuration = contractDuration;
                choiceLetter.mercenaryName = mercenaryName;
                choiceLetter.StartTimeout(60000); // 24 hours

                Find.LetterStack.ReceiveLetter(choiceLetter, null, 0, true);
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error sending mercenary offer: " + ex.Message);
            }
        }

        private string GetRandomMercenaryName()
        {
            try
            {
                // Get available mercenary names from the list
                List<string> availableMercenaries = GetAvailableMercenaries();

                if (availableMercenaries.Count == 0)
                {
                    // Fallback to basic mercenary if no custom types are available
                    return "Veteran";
                }

                return availableMercenaries.RandomElement();
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error getting random mercenary name: " + ex.Message);
                return "Veteran";
            }
        }

        private List<string> GetAvailableMercenaries()
        {
            // Get available pawn names from PawnEditor XML files
            var availablePawns = SpecificPawnLoader.GetAvailablePawnNames();
            
            if (availablePawns.Count == 0)
            {
                Log.Warning("MSSFP: No PawnEditor XML files found. Using fallback mercenaries.");
                return new List<string> { "Veteran" };
            }

            return availablePawns;
        }
    }
}
