using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace MSSFP.Questing;

public class SitePartWorker_LoversAdvance : SitePartWorker
{
    public override void Init(Site site, SitePart sitePart)
    {
        sitePart.conditionCauser = ThingMaker.MakeThing(MSSFPDefOf.MSSFP_ConstructionOffice, GenStuff.RandomStuffFor(MSSFPDefOf.MSSFP_ConstructionOffice));
    }

    public static Lazy<MethodInfo> Tick => new(() => AccessTools.Method(typeof(Entity), "Tick"));

    public override void SitePartWorkerTick(SitePart sitePart)
    {
        if (sitePart.conditionCauser.DestroyedOrNull() || sitePart.conditionCauser.Spawned)
            return;
        Tick.Value.Invoke(sitePart.conditionCauser, null);
    }

    public override void Notify_SiteMapAboutToBeRemoved(SitePart sitePart)
    {
        base.Notify_SiteMapAboutToBeRemoved(sitePart);
        if (sitePart.conditionCauser.DestroyedOrNull() || !sitePart.conditionCauser.Spawned || sitePart.conditionCauser.Map != sitePart.site.Map)
            return;
        sitePart.conditionCauser.DeSpawn(DestroyMode.Vanish);
        sitePart.conditionCauserWasSpawned = false;
    }

    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        slate.Set("conditionCauser", part.conditionCauser);
    }
}
