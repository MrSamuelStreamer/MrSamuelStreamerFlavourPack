using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using PawnGraphicUtils = MSSFP.Utils.PawnGraphicUtils;

namespace MSSFP.Hediffs;

[StaticConstructorOnStartup]
public class HediffComp_Haunt : HediffComp
{
    public static Texture2D icon = ContentFinder<Texture2D>.Get("UI/MSS_FP_Haunts_Toggle");
    public Pawn pawnToDraw;
    public string name;
    public string texPath;
    public Texture2D pawnTexture;

    public SkillDef skillToBoost = null;
    public int SkillBoostLevel = 0;

    public virtual string PawnName => name;

    public int OnUntilTick = -1;
    public int OffUntilTick = -1;

    public int NextProxCheck = -1;

    public virtual Texture2D PawnTexture
    {
        get
        {
            if (pawnTexture == null)
            {
                pawnTexture = PawnGraphicUtils.LoadTexture(
                    Path.Combine(PawnGraphicUtils.SaveDataPath, TexPath)
                );
            }
            return pawnTexture;
        }
    }

    public virtual string TexPath
    {
        get => texPath;
    }

    public virtual IEnumerable<string> GetAllTexPaths()
    {
        if (texPath != null)
            yield return texPath;
    }

    /// <summary>
    /// Returns all skill boosts this haunt provides. Used by HauntsCache.RebuildCacheForPawn
    /// to build the cache that SkillRecord_Patch reads. Override in subclasses that track
    /// multiple skill boosts (e.g. body-hop history).
    /// </summary>
    public virtual IEnumerable<(SkillDef skill, int boost)> GetSkillBoosts()
    {
        if (skillToBoost != null && SkillBoostLevel > 0)
            yield return (skillToBoost, SkillBoostLevel);
    }

    protected Dictionary<SkillDef, int> aptitudesCached = new Dictionary<SkillDef, int>();

    private HediffCompProperties_Haunt Props => props as HediffCompProperties_Haunt;

    public override string CompLabelInBracketsExtra
    {
        get
        {
            if (PawnName != null)
                return "MSS_FP_HauntedBy".Translate(PawnName);
            return pawnToDraw == null
                ? null
                : "MSS_FP_HauntedBy".Translate(pawnToDraw.NameShortColored);
        }
    }

    public override string CompDescriptionExtra
    {
        get
        {
            if (pawnToDraw == null)
                return null;
            if (skillToBoost != null && SkillBoostLevel > 0)
            {
                return pawnToDraw == null
                    ? null
                    : "\n\n"
                        + "MSS_FP_HauntedBuff".Translate(
                            pawnToDraw.NameShortColored,
                            skillToBoost.LabelCap,
                            SkillBoostLevel
                        );
            }
            return pawnToDraw == null
                ? null
                : "\n\n"
                    + "MSS_FP_HauntedUnBuff".Translate(
                        pawnToDraw.NameShortColored,
                        parent.pawn.NameShortColored
                    );
        }
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (Props.CanTransferInProximity && NextProxCheck < Find.TickManager.TicksGame)
        {
            NextProxCheck = Find.TickManager.TicksGame + Props.ProximityTransferCheckTicks;

            if (!Rand.Chance(Props.ProximityTransferChancePerCheck))
                return;
            Pawn pawn = GenRadial
                .RadialCellsAround(parent.pawn.Position, Props.ProximityRadius, true)
                .SelectMany(cell =>
                    parent
                        .pawn.Map.thingGrid.ThingsAt(cell)
                        .OfType<Pawn>()
                        .Except([parent.pawn])
                        .Where(p => p.RaceProps.Humanlike)
                )
                .RandomElementWithFallback();

            if (pawn != null)
            {
                Pawn sourcePawn = parent.pawn;
                float severity = parent.Severity;

                Hediff newHediff = HediffMaker.MakeHediff(parent.def, pawn);
                newHediff.Severity = severity;

                HediffComp_Haunt newHauntComp = newHediff.TryGetComp<HediffComp_Haunt>();
                if (newHauntComp != null)
                {
                    newHauntComp.pawnToDraw = pawnToDraw;
                    newHauntComp.name = name;
                    newHauntComp.texPath = texPath;
                    newHauntComp.pawnTexture = pawnTexture;
                    newHauntComp.skillToBoost = skillToBoost;
                    newHauntComp.SkillBoostLevel = SkillBoostLevel;
                    newHauntComp.OnUntilTick = OnUntilTick;
                    newHauntComp.OffUntilTick = OffUntilTick;
                    newHauntComp.NextProxCheck = NextProxCheck;
                }

                // Add to new pawn first, then remove from old — prevents data loss
                // if interrupted. Brief double-haunt window is harmless.
                pawn.health.AddHediff(newHediff);
                sourcePawn.health.RemoveHediff(parent);

                // Skill boosts applied via SkillRecord_Patch + HauntsCache — no
                // direct mutation needed. Cache is rebuilt by CompPostMake/CompPostPostRemoved.
            }
        }
    }

    public bool ShouldDisplayNow()
    {
        if (Props.AlwaysOn)
            return true;

        if (OffUntilTick < 0)
            OffUntilTick = Find.TickManager.TicksGame + Props.OffTimeTicksRange.RandomInRange;

        if (OnUntilTick < OffUntilTick && OnUntilTick > Find.TickManager.TicksGame)
        {
            if (OffUntilTick < OnUntilTick)
            {
                OffUntilTick = OnUntilTick + Props.OffTimeTicksRange.RandomInRange;
            }

            return true;
        }
        if (OffUntilTick > Find.TickManager.TicksGame)
        {
            if (OnUntilTick < OffUntilTick)
            {
                OnUntilTick = OffUntilTick + Props.OnTimeTicksRange.RandomInRange;
            }

            return false;
        }

        return false;
    }

    public virtual void DrawAt(Vector3 drawPos)
    {
        if (!MSSFPMod.settings.ShowHaunts)
            return;
        if (!ShouldDisplayNow())
            return;

        if (Props.onlyRenderWhenDrafted && Pawn.drafter is not { Drafted: true })
        {
            return;
        }

        if (Props.graphicData.Graphic is PawnHauntGraphic && TexPath == null)
            return;

        if (Props.graphicData?.Graphic is PawnHauntGraphic gfx && TexPath != null)
        {
            Texture2D tex = PawnTexture;
            if (tex == null)
                return;
            gfx.SetOverrideMaterial(tex);
        }

        Vector3 offset = new();

        if (Props.offsets != null)
        {
            if (Props.offsets.Count == 4)
            {
                offset = Props.offsets[Pawn.Rotation.AsInt];
            }
            else
            {
                offset = Props.offsets[0];
            }
        }

        Rot4 rot = Pawn.Rotation;
        if (Props.graphicData is { Graphic: PawnHauntGraphic })
        {
            rot = Rot4.North;
        }
        Props.graphicData?.Graphic.Draw(
            new Vector3(drawPos.x, AltitudeLayer.Pawn.AltitudeFor(), drawPos.z) + offset,
            rot,
            pawnToDraw ?? parent.pawn
        );
    }

    public override void CompPostMake()
    {
        base.CompPostMake();
        HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
        NextProxCheck =
            Find.TickManager.TicksGame
            + Props.ProximityTransferCheckTicks
            + Rand.Range(0, GenDate.TicksPerHour);

        HauntsCache.RebuildCacheForPawn(Pawn);
    }

    public virtual void SetPawnToDraw(Pawn pawn)
    {
        if (pawn == null)
            return;

        pawnToDraw = pawn;
        aptitudesCached.Clear();
        texPath = PawnGraphicUtils.SavePawnTexture(pawn);
        pawnTexture = null; // force PawnTexture property to reload from disk on next render

        if (pawnToDraw.skills != null)
        {
            SkillRecord maxSkill = pawnToDraw.skills.skills.MaxBy(pawnSkill => pawnSkill.Level);

            skillToBoost = maxSkill.def;
            SkillBoostLevel = Mathf.CeilToInt(maxSkill.Level / 3f);
        }

        // Skill boost is applied via SkillRecord_Patch reading from HauntsCache
        HauntsCache.RebuildCacheForPawn(Pawn);

        if (Pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(Props.thought) is { } thought)
        {
            thought.otherPawn = pawnToDraw;
        }
        else
        {
            TryAddMemory();
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        HauntsCache.RemoveHaunt(Pawn.thingIDNumber, this);

        if (Props.thought != null)
            Pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(Props.thought);

        // Skill boost removal happens automatically — RebuildCacheForPawn will no longer
        // include this comp's contribution, and SkillRecord_Patch reads from the cache.
        HauntsCache.RebuildCacheForPawn(Pawn);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();

        Scribe_References.Look(ref pawnToDraw, "pawnToDraw");
        Scribe_Values.Look(ref texPath, "texPath");
        Scribe_Values.Look(ref name, "name");
        Scribe_Values.Look(ref OnUntilTick, "OnUntilTick");
        Scribe_Values.Look(ref OffUntilTick, "OffUntilTick");
        Scribe_Values.Look(ref NextProxCheck, "NextProxCheck");

        Scribe_Defs.Look(ref skillToBoost, "skillToBoost");
        Scribe_Values.Look(ref SkillBoostLevel, "SkillBoostLevel", 0);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
            HauntsCache.AddHaunt(Pawn.thingIDNumber, this);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        Command_Target showPawn = new Command_Target();
        showPawn.defaultLabel = "Select Pawn To Draw";
        showPawn.targetingParams = TargetingParameters.ForColonist();
        showPawn.icon = icon;
        showPawn.action = (
            info =>
            {
                SetPawnToDraw(info.Pawn);
            }
        );

        yield return showPawn;
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        TryAddMemory();
        HauntsCache.RebuildCacheForPawn(Pawn);
    }

    public override void Notify_Spawned()
    {
        TryAddMemory();
        HauntsCache.RebuildCacheForPawn(Pawn);
    }

    private void TryAddMemory()
    {
        if (pawnToDraw is null)
            return;
        if (Props.thought == null)
            return;
        if (Pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(Props.thought) != null)
            return;
        Thought_Memory newThought = (Thought_Memory)ThoughtMaker.MakeThought(Props.thought);
        newThought.permanent = true;
        Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought, pawnToDraw);
    }
}
