using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Comps.Map
{
    public class MercenaryContractMapComponent : MapComponent
    {
        public class MercenaryContract : IExposable
        {
            public List<Pawn> mercenaries;
            public int contractEndTick;
            public int totalCost;

            public MercenaryContract() { }

            public MercenaryContract(List<Pawn> mercenaries, int contractEndTick, int totalCost)
            {
                this.mercenaries = mercenaries;
                this.contractEndTick = contractEndTick;
                this.totalCost = totalCost;
            }

            public void ExposeData()
            {
                Scribe_Collections.Look(ref mercenaries, "mercenaries", LookMode.Reference);
                Scribe_Values.Look(ref contractEndTick, "contractEndTick");
                Scribe_Values.Look(ref totalCost, "totalCost");
            }
        }

        // Active mercenary contracts
        public List<MercenaryContract> activeContracts = new();

        // Timing for contract checks
        private int nextCheckTick = 0;

        public MercenaryContractMapComponent(Verse.Map map)
            : base(map) { }

        public bool HasActiveContract => activeContracts.Count > 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref activeContracts, "activeContracts", LookMode.Deep);
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", 0);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                activeContracts ??= new();
            }
        }

        public void StartContract(List<Pawn> mercenaries, int duration, int cost)
        {
            // Add validation
            if (mercenaries == null || mercenaries.Count == 0)
            {
                Log.Warning("MSSFP: Attempted to start contract with null or empty mercenary list");
                return;
            }

            if (duration <= 0 || cost <= 0)
            {
                Log.Warning(
                    "MSSFP: Invalid contract parameters - duration: " + duration + ", cost: " + cost
                );
                return;
            }

            int contractEndTick = Find.TickManager.TicksGame + duration;
            MercenaryContract contract = new(mercenaries, contractEndTick, cost);
            activeContracts.Add(contract);
        }

        public override void MapComponentTick()
        {
            if (!MSSFPMod.settings.EnableMercenaryHiring)
                return;

            if (Find.TickManager.TicksGame < nextCheckTick)
                return;

            nextCheckTick = Find.TickManager.TicksGame + 600; // Schedule next check in 10 seconds
            CheckForExpiredContracts();
        }

        private void CheckForExpiredContracts()
        {
            List<MercenaryContract> expiredContracts = new();

            foreach (MercenaryContract contract in activeContracts)
            {
                if (Find.TickManager.TicksGame >= contract.contractEndTick)
                {
                    expiredContracts.Add(contract);
                }
            }

            foreach (MercenaryContract contract in expiredContracts)
            {
                EndContract(contract);
            }
        }

        private void EndContract(MercenaryContract contract)
        {
            RemoveMercenariesFromMap(contract);
            SendContractEndedNotification(contract);
            activeContracts.Remove(contract);
        }

        private void RemoveMercenariesFromMap(MercenaryContract contract)
        {
            foreach (Pawn mercenary in contract.mercenaries)
            {
                if (mercenary != null && !mercenary.Dead && mercenary.Map == map)
                {
                    RemoveMercenaryFromFaction(mercenary);
                }
            }
        }

        private void RemoveMercenaryFromFaction(Pawn mercenary)
        {
            try
            {
                if (mercenary == null)
                    return;

                // Remove from player faction
                mercenary.SetFaction(null);

                // Give them a job to walk to the map edge
                if (
                    mercenary.jobs != null
                    && CellFinder.TryFindRandomEdgeCellWith(
                        c => map.reachability.CanReachColony(c) && !c.Fogged(map),
                        map,
                        CellFinder.EdgeRoadChance_Ignore,
                        out IntVec3 exitCell
                    )
                )
                {
                    Job exitJob = JobMaker.MakeJob(JobDefOf.Goto, exitCell);
                    mercenary.jobs.StartJob(exitJob, JobCondition.InterruptForced);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error removing mercenary from faction: " + ex.Message);
            }
        }

        private void SendContractEndedNotification(MercenaryContract contract)
        {
            try
            {
                string mercenaryNames = GetMercenaryNames(contract.mercenaries);

                Find.LetterStack.ReceiveLetter(
                    "MSSFP_MercenaryContractEndedLabel".Translate(),
                    "MSSFP_MercenaryContractEndedText".Translate(mercenaryNames),
                    LetterDefOf.NeutralEvent
                );
            }
            catch (System.Exception ex)
            {
                Log.Error("MSSFP: Error sending contract ended notification: " + ex.Message);
            }
        }

        private string GetMercenaryNames(List<Pawn> mercenaries)
        {
            return string.Join(
                ", ",
                mercenaries.Where(p => p != null).Select(p => p.NameShortColored)
            );
        }

        public void CancelContract(MercenaryContract contract)
        {
            // Early termination - mercenaries leave immediately
            EndContract(contract);
        }

        public List<Pawn> GetAllHiredMercenaries()
        {
            return activeContracts
                .SelectMany(c => c.mercenaries)
                .Where(p => p != null && !p.Dead && p.Map == map)
                .ToList();
        }
    }
}
