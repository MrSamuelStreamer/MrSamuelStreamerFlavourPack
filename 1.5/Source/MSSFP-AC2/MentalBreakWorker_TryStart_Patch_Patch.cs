using AlteredCarbon;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.AC2;

[HarmonyPatch(typeof(MentalBreakWorker_TryStart_Patch))]
public static class MentalBreakWorker_TryStart_Patch_Patch
{
    public static HediffDef AC_MentalFuse => HediffDef.Named("AC_MentalFuse");

    [HarmonyPatch(nameof(MentalBreakWorker_TryStart_Patch.Prefix))]
    [HarmonyPostfix]
    public static void MentalBreakWorker_TryStart_Patch_Prefix_Postfix(Pawn pawn)
    {
        if(!MSSFPMod.settings.SingleUseMentalFuses) return;

        Hediff acMentalFuse = pawn.health.hediffSet.GetFirstHediffOfDef(AC_MentalFuse);

        if (acMentalFuse is not null)
        {
            pawn.health.RemoveHediff(acMentalFuse);
            Messages.Message($"{pawn.NameShortColored}'s mental fuse has been destroyed", MessageTypeDefOf.NegativeHealthEvent, true);

            Effecter effecter = EffecterDefOf.ExtinguisherExplosion.Spawn();
            effecter.Trigger(new TargetInfo(pawn.Position, pawn.MapHeld), new TargetInfo(pawn.Position, pawn.MapHeld));
            effecter.Cleanup();
            GenExplosion.DoExplosion(pawn.Position, pawn.MapHeld, 2, DamageDefOf.Extinguish, null, explosionSound: SoundDefOf.Explosion_FirefoamPopper, postExplosionSpawnThingDef: ThingDefOf.Filth_FireFoam, postExplosionSpawnChance: 1f, applyDamageToExplosionCellsNeighbors: true);
        }
    }
}
