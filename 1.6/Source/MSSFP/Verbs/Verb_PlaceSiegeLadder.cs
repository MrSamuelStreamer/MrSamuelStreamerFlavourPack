using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MSSFP.Verbs
{
    public class Verb_PlaceSiegeLadder : Verb_CastTargetEffectLances
    {
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return base.CanHitTarget(targ);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return target.IsValid
                && target.Thing is Building
                && base.ValidateTarget(target, showMessages);
        }

        protected override bool TryCastShot()
        {
            if (currentTarget.IsValid && currentTarget.Thing is Building wall)
            {
                var comp =
                    this.EquipmentSource?.GetComp<MSSFP.Comps.CompTargetEffect_PlaceSiegeLadder>();
                if (comp != null)
                {
                    if (wall is MSSFP.Buildings.Building_ClimbableWallProxy)
                    {
                        Messages.Message(
                            "MSS_SiegeLadder_AlreadyPlaced".Translate(),
                            MessageTypeDefOf.RejectInput,
                            false
                        );
                        return false;
                    }

                    if (!comp.CanClimbOverWall(CasterPawn, wall))
                    {
                        Messages.Message(
                            "MSS_SiegeLadder_FarSideRoofed".Translate(),
                            MessageTypeDefOf.RejectInput,
                            false
                        );
                        return false;
                    }

                    if (comp.HasWallBehind(CasterPawn, wall))
                    {
                        Messages.Message(
                            "Cannot place ladder - there are multiple walls in a row.",
                            MessageTypeDefOf.RejectInput,
                            false
                        );
                        return false;
                    }
                }
            }

            return base.TryCastShot();
        }
    }
}
