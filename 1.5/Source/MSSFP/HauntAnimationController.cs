using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class HauntAnimationController(Map map) : MapComponent(map)
{
    public Map Map => map;

    public bool HauntingActive = false;
    public Pawn Target;

    public FloatRange LiftRange = new(1f, 10f);
    public IntRange SpinRange = new(1, 10);

    public class HauntingParams(float liftHeightHeight, int timesToSpin) : IExposable
    {
        public float LiftHeight = liftHeightHeight;
        public int TimesToSpin = timesToSpin;

        public void ExposeData()
        {
            Scribe_Values.Look(ref LiftHeight, "LiftHeight");
            Scribe_Values.Look(ref TimesToSpin, "TimesToSpin");
        }
    }

    public Dictionary<HauntedThingFlyer, HauntingParams> HauntedThings;

    public enum HauntPhase : byte
    {
        Lifting,
        Spinning,
        Dropping,
    }

    public HauntPhase CurrentPhase = HauntPhase.Lifting;
    public int PhaseTicks = 0;
    public int StartPhaseTick = 0;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref HauntingActive, "HauntingActive", HauntingActive);
        Scribe_References.Look(ref Target, "Target");
        Scribe_Collections.Look(ref HauntedThings, "HauntedThings", LookMode.Reference, LookMode.Deep);
        Scribe_Values.Look(ref PhaseTicks, "PhaseTicks", PhaseTicks);
        Scribe_Values.Look(ref StartPhaseTick, "StartPhaseTick", StartPhaseTick);
        Scribe_Values.Look(ref CurrentPhase, "CurrentPhase", CurrentPhase);
    }

    public void StartHaunting(Pawn target, int radius = 10, float hauntChance = 0.5f, int minItems = 5, int ticks = 1500)
    {
        if (HauntingActive)
            return;

        int HauntUntil = Find.TickManager.TicksGame + ticks;
        PhaseTicks = HauntUntil / 3;
        StartPhaseTick = Find.TickManager.TicksGame;

        var cells = GenRadial.RadialCellsAround(target.Position, radius, true);
        List<Thing> things = cells.SelectMany(c => Map.thingGrid.ThingsAt(c)).ToList();

        List<Thing> selectedThings = things.Take(minItems).ToList();

        selectedThings.AddRange(things.Except(selectedThings).Where(t => Rand.Chance(hauntChance)));

        HauntedThings = new Dictionary<HauntedThingFlyer, HauntingParams>();

        foreach (Thing selectedThing in selectedThings)
        {
            HauntedThingFlyer flyer = ThingMaker.MakeThing(MSSFPDefOf.MSSFP_HauntedThingFlyer, null) as HauntedThingFlyer;
            GenSpawn.Spawn(flyer, selectedThing.Position, map);
            flyer!.AddThing(selectedThing);

            HauntedThings[flyer] = new HauntingParams(LiftRange.RandomInRange, SpinRange.RandomInRange);
        }

        HauntingActive = true;
    }

    public void LiftPhaseTick()
    {
        if (StartPhaseTick + PhaseTicks < Find.TickManager.TicksGame)
        {
            CurrentPhase++;
            return;
        }

        foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        {
            Vector3 EndPos = new(0, 0, hauntedThing.Value.LiftHeight);
            float ratio = (float)(Find.TickManager.TicksGame - StartPhaseTick) / PhaseTicks;
            hauntedThing.Key.SetPositionDirectly(Vector3.Lerp(hauntedThing.Key.StartPosition, hauntedThing.Key.StartPosition + EndPos, ratio));
        }
    }

    public void SpinPhaseTick()
    {
        if (StartPhaseTick + PhaseTicks + PhaseTicks < Find.TickManager.TicksGame)
        {
            CurrentPhase++;
            return;
        }

        // foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        // {
        //     Vector3 EndPos = new(0, 0, hauntedThing.Value.LiftHeight);
        //     float ratio = (float)(Find.TickManager.TicksGame - StartPhaseTick) / PhaseTicks;
        //     hauntedThing.Key.SetPositionDirectly(Vector3.Lerp(hauntedThing.Key.StartPosition, hauntedThing.Key.StartPosition + EndPos, ratio));
        // }
    }

    public void DropPhaseTick()
    {
        if (StartPhaseTick + PhaseTicks + PhaseTicks + PhaseTicks < Find.TickManager.TicksGame)
        {
            HauntingActive = false;
            foreach (HauntedThingFlyer hauntedThingFlyer in HauntedThings.Keys.ToList())
            {
                HauntedThings.Remove(hauntedThingFlyer);
                hauntedThingFlyer.Destroy();
            }

            CurrentPhase = HauntPhase.Lifting;
            PhaseTicks = 0;
            StartPhaseTick = 0;

            Target = null;

            return;
        }

        foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        {
            Vector3 EndPos = new(0, 0, hauntedThing.Value.LiftHeight);
            float ratio = (float)(Find.TickManager.TicksGame - (StartPhaseTick + PhaseTicks + PhaseTicks)) / PhaseTicks;
            hauntedThing.Key.SetPositionDirectly(Vector3.Lerp(hauntedThing.Key.StartPosition + EndPos, hauntedThing.Key.StartPosition, ratio));
        }
    }

    public override void MapComponentTick()
    {
        if (HauntingActive)
        {
            switch (CurrentPhase)
            {
                case HauntPhase.Lifting:
                    LiftPhaseTick();
                    break;
                case HauntPhase.Spinning:
                    SpinPhaseTick();
                    break;
                case HauntPhase.Dropping:
                    DropPhaseTick();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
