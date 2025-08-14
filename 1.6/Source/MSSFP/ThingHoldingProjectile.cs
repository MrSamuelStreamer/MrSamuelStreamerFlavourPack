using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MSSFP;

public class ThingHoldingProjectile : Bullet, IThingHolder
{
    protected ThingOwner innerContainer;

    public Lazy<MethodInfo> NotifyImpactInfo = new Lazy<MethodInfo>(() =>
        AccessTools.Method(typeof(Bullet), "NotifyImpact")
    );

    public Thing HeldThing
    {
        get { return innerContainer.Count <= 0 ? null : innerContainer[0]; }
        set
        {
            if (value.holdingOwner != null)
            {
                value.holdingOwner.TryTransferToContainer(value, innerContainer, value.stackCount);
            }
            else
                innerContainer.TryAdd(value);

            if (Graphic is ThingHoldingProjectileGraphicPassthrough graphic)
            {
                graphic.HeldThing = value;
            }
        }
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        innerContainer = new ThingOwner<Thing>(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", (object)this);
    }

    public override int DamageAmount
    {
        get
        {
            int baseDamage = base.DamageAmount;
            return HeldThing == null
                ? 0
                : Math.Max(
                    2,
                    Mathf.RoundToInt(baseDamage * HeldThing.GetStatValue(StatDefOf.Mass))
                );
        }
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = Map;
        IntVec3 position = Position;

        GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);

        BattleLogEntry_RangedImpact entryRangedImpact = new BattleLogEntry_RangedImpact(
            launcher,
            hitThing,
            intendedTarget.Thing,
            equipmentDef,
            def,
            targetCoverDef
        );
        Find.BattleLog.Add(entryRangedImpact);
        NotifyImpactInfo.Value.Invoke(this, [hitThing, map, position]);

        if (hitThing != null)
        {
            bool instigatorGuilty = launcher is not Pawn { Drafted: true };
            DamageInfo dinfo1 = new DamageInfo(
                def.projectile.damageDef,
                DamageAmount,
                ArmorPenetration,
                ExactRotation.eulerAngles.y,
                launcher,
                // weapon: equipmentDef,
                weapon: HeldThing.def,
                intendedTarget: intendedTarget.Thing,
                instigatorGuilty: instigatorGuilty
            );
            // dinfo1.SetWeaponQuality(equipmentQuality);
            hitThing.TakeDamage(dinfo1).AssociateWithLog(entryRangedImpact);

            (hitThing as Pawn)?.stances?.stagger.Notify_BulletImpact(this);
        }
        else
        {
            if (!blockedByShield)
            {
                SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(Position, map));
                if (Position.GetTerrain(map).takeSplashes)
                    FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
                else
                    FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
            }
        }

        innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        Destroy(DestroyMode.Vanish);
    }

    public ThingOwner GetDirectlyHeldThings() => this.innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(
            outChildren,
            (IList<Thing>)this.GetDirectlyHeldThings()
        );
    }
}
