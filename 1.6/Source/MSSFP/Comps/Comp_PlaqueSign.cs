using MSSFP.Pawns;
using RimWorld;
using Verse;

namespace MSSFP.Comps
{
    public class Comp_PlaqueSign : ThingComp
    {
        private DynamicPawnStorage pawnStorage;

        public string OriginalPawnName => pawnStorage?.OriginalPawnName;
        public bool HasStoredPawn => pawnStorage?.HasStoredPawn ?? false;

        public CompProperties_PlaqueSign Props => (CompProperties_PlaqueSign)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDestroy(DestroyMode mode, Verse.Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            if (HasStoredPawn && previousMap != null)
                RestorePawn(previousMap);
        }

        public bool StorePawn(Pawn pawn)
        {
            if (pawnStorage == null)
                pawnStorage = new DynamicPawnStorage();

            var success = pawnStorage.StorePawn(pawn);

            if (success)
                Messages.Message(
                    $"{pawn.NameShortColored} was transformed into a sign by the plaque gun!",
                    MessageTypeDefOf.NeutralEvent
                );
            else
                Messages.Message(
                    $"Failed to transform {pawn.NameShortColored} into a sign!",
                    MessageTypeDefOf.RejectInput
                );

            return success;
        }

        private void RestorePawn(Verse.Map map)
        {
            if (!HasStoredPawn || pawnStorage == null || map == null)
                return;

            try
            {
                var restoredPawn = pawnStorage.RestorePawn(parent.Position, map);

                if (restoredPawn != null)
                    Messages.Message(
                        $"{OriginalPawnName} has been restored from the plaque sign!",
                        restoredPawn,
                        MessageTypeDefOf.PositiveEvent
                    );
                else
                    Messages.Message(
                        $"Failed to restore {OriginalPawnName} from the plaque sign!",
                        MessageTypeDefOf.RejectInput
                    );
            }
            catch (System.Exception ex)
            {
                Log.Error(
                    $"MSSFP: Failed to restore pawn {OriginalPawnName} from plaque sign: {ex.Message}"
                );
            }
        }

        public override string CompInspectStringExtra()
        {
            return pawnStorage?.GetStorageDescription() ?? base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref pawnStorage, "pawnStorage");
        }
    }
}
