﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

public class JobGiver_EatGems : ThinkNode_JobGiver
{
    public List<ThingDef> EdibleGems;

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (MSSFPMod.settings.disableFroggeNom) return null;
        if (EdibleGems.EnumerableNullOrEmpty()) return null;
        if(pawn.Map == null) return null;

        IEnumerable<Thing> validThings = pawn.Map.listerThings.GetAllThings((thing) => EdibleGems.Contains(thing.def), true);

        Thing target = validThings.Where(thing=>thing.stackCount > 0).RandomElementByWeight((thing) => thing.stackCount * thing.def.BaseMarketValue);

        if(target == null) return null;

        if (!pawn.CanReserveAndReach((LocalTargetInfo)target, PathEndMode.Touch, Danger.Deadly) || target.IsBurning())
            return null;

        int count = PawnUtility.GetMaxAllowedToPickUp(pawn, target.def);

        if(count == 0) return null;

        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, (LocalTargetInfo) target);
        job.count = Math.Min(count, target.stackCount);
        job.checkEncumbrance = true;
        job.takeInventoryDelay = 120;
        job.ignoreForbidden = true;
        return job;
    }
}
