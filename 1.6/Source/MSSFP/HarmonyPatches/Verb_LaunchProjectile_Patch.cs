using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Verbs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Allows a projectile to drop a held thing
/// </summary>
[HarmonyPatch(typeof(Verb_LaunchProjectile))]
public static class Verb_LaunchProjectile_Patch
{
    public static void ModifyProjectile(Verb_LaunchProjectile instance, Projectile projectile)
    {
        if (instance is not Verb_AbilityShootThingHolder verb)
            return;
        verb.ModifyProjectile(projectile);
    }

    [HarmonyPatch("TryCastShot")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

        // Find the index of the instruction where `projectile2` is stored (stloc.s projectile2)
        int insertIndex = codes.FindIndex(ci =>
            ci.opcode == OpCodes.Stloc_S
            && ci.operand is LocalBuilder lb
            && lb.LocalType == typeof(Projectile)
        );

        if (insertIndex == -1)
        {
            // Log an error if the injection index could not be located
            Log.Error("Could not locate the `projectile2` storage in IL for TryCastShot.");
            return codes;
        }

        // Insert the call to ModifyProjectile after the `stloc.s projectile2`
        List<CodeInstruction> newInstructions = new List<CodeInstruction>
        {
            // Load `this` (Verb_LaunchProjectile instance)
            new CodeInstruction(OpCodes.Ldarg_0),
            // Load `projectile2` from the local variable
            new CodeInstruction(OpCodes.Ldloc_S, codes[insertIndex].operand),
            // Call the ModifyProjectile method
            new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(
                    typeof(MSSFP.HarmonyPatches.Verb_LaunchProjectile_Patch),
                    nameof(Verb_LaunchProjectile_Patch.ModifyProjectile)
                )
            ),
        };

        // Insert the new instructions after `stloc.s projectile2`
        codes.InsertRange(insertIndex + 1, newInstructions);

        return codes;
    }

    [HarmonyPatch("TryCastShot")]
    [HarmonyPostfix]
    public static void TryCastShot_Postfix(Verb_LaunchProjectile __instance, ref bool __result)
    {
        if(!__result) return;

        Thing weapon = __instance.EquipmentSource;
        float weaponMass = weapon.GetStatValue(StatDefOf.Mass);

        if(__instance.caster is not Pawn caster) return;

        float casterSize = caster.BodySize;

        float ratio = weaponMass / casterSize;

        if (MSSFPMod.settings.EnableRecoilDamage)
        {
            caster.TakeDamage(new DamageInfo(DamageDefOf.Blunt, ratio * MSSFPMod.settings.RecoilDamageMultiplier, instigator: caster, weapon: weapon.def));
        }

        if (MSSFPMod.settings.EnableRecoilKnockback)
        {
            IntVec3 casterPosition = caster.Position;
            IntVec3 targetPosition = __instance.CurrentTarget.Cell;

            AbilityUtility.DoClamor(caster.Position, 1, caster, ClamorDefOf.Impact);
            FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3(), caster.Map, Rand.Range(1.5f, 3f), CompAbilityEffect_Chunkskip.DustColor);

            Vector3 direction = (targetPosition - casterPosition).ToVector3();
            direction.Normalize();
            Vector3 knockbackVector = -direction * ratio * MSSFPMod.settings.RecoilKnockbackMultiplier;
            IntVec3 knockbackPosition = (casterPosition.ToVector3() + knockbackVector).ToIntVec3();

            caster.Position = knockbackPosition;
            caster.SetPositionDirect(knockbackPosition);
            caster.pather.ResetToCurrentPosition();
            caster.Notify_Teleported();

            AbilityUtility.DoClamor(caster.Position, 1, caster, ClamorDefOf.Impact);

        }
    }
}
