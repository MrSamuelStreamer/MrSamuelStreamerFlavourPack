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
    public HauntedThingFlyer TargetFlyer;
    public Vector3 StartPos;

    public FloatRange LiftRange = new(1f, 10f);
    public IntRange SpinRange = new(1, 10);

    public WeatherDef originalWeather;

    public int ticksSinceLastEmitted;
    protected Mote mote;

    public class HauntingParams(float liftHeightHeight, int timesToSpin) : IExposable
    {
        public float LiftHeight = liftHeightHeight;
        public int TimesToSpin = timesToSpin;
        public float Radius;
        public Vector3 RotationStart;
        public Vector3 EndSpinPosition;

        public Vector3 CurrentRotatedPos(int PhaseLength, int currentPhaseTick)
        {
            int ticksPerRotation = PhaseLength / TimesToSpin;
            float anglePerTick = Mathf.Deg2Rad * (360f / ticksPerRotation);
            int ticksIntoCurrentRotation = currentPhaseTick % ticksPerRotation;

            float currentAngle = anglePerTick * ticksIntoCurrentRotation;

            return RotationStart + new Vector3(Mathf.Sin(currentAngle) * Radius, 0, Mathf.Cos(currentAngle) * Radius);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref LiftHeight, "LiftHeight");
            Scribe_Values.Look(ref TimesToSpin, "TimesToSpin");
            Scribe_Values.Look(ref Radius, "Radius");
            Scribe_Values.Look(ref RotationStart, "RotationStart");
            Scribe_Values.Look(ref EndSpinPosition, "EndSpinPosition");
        }
    }

    public Dictionary<HauntedThingFlyer, HauntingParams> HauntedThings;

    public Vector3 EpicenterOffset = new Vector3(0, 0, 5);

    public Vector3 Epicenter => StartPos + EpicenterOffset;

    public enum HauntPhase : byte
    {
        Lifting,
        Spinning,
        Dropping,
    }

    public HauntPhase CurrentPhase = HauntPhase.Lifting;
    public int LiftPhaseTicks = 0;
    public int SpinPhaseTicks = 0;
    public int DropPhaseTicks = 0;
    public int StartPhaseTick = 0;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref HauntingActive, "HauntingActive", HauntingActive);
        Scribe_References.Look(ref Target, "Target");
        Scribe_Collections.Look(ref HauntedThings, "HauntedThings", LookMode.Reference, LookMode.Deep);
        Scribe_Values.Look(ref LiftPhaseTicks, "LiftPhaseTicks", LiftPhaseTicks);
        Scribe_Values.Look(ref SpinPhaseTicks, "SpinPhaseTicks", SpinPhaseTicks);
        Scribe_Values.Look(ref DropPhaseTicks, "DropPhaseTicks", DropPhaseTicks);
        Scribe_Values.Look(ref StartPhaseTick, "StartPhaseTick", StartPhaseTick);
        Scribe_Defs.Look(ref originalWeather, "originalWeather");
        Scribe_Values.Look(ref CurrentPhase, "CurrentPhase", CurrentPhase);
        Scribe_References.Look(ref TargetFlyer, "TargetFlyer");
        Scribe_Values.Look(ref ticksSinceLastEmitted, "ticksSinceLastEmitted", 0);
    }

    public void StartHaunting(Pawn target, int radius = 10, float hauntChance = 0.45f, int minItems = 5, int ticks = 1000)
    {
        if (HauntingActive)
            return;

        originalWeather = this.map.weatherManager.curWeather;
        map.weatherManager.TransitionTo(DefDatabase<WeatherDef>.GetNamed("RainyThunderstorm"));
        map.weatherManager.curWeatherAge = 3940;
        Target = target;

        ticksSinceLastEmitted = 0;

        LiftPhaseTicks = ticks / 6;
        DropPhaseTicks = ticks / 6;
        SpinPhaseTicks = ticks - LiftPhaseTicks - DropPhaseTicks;

        StartPhaseTick = Find.TickManager.TicksGame;

        Target.stances.stunner.StunFor(ticks, Target, true, true, false);

        IEnumerable<IntVec3> cells = GenRadial.RadialCellsAround(Target.Position, radius, true);
        List<Thing> things = cells.SelectMany(c => Map.thingGrid.ThingsAt(c)).Except([Target]).ToList();

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
        StartPos = Target.DrawPos;

        TargetFlyer = ThingMaker.MakeThing(MSSFPDefOf.MSSFP_HauntedThingTargetFlyer, null) as HauntedThingFlyer;
        GenSpawn.Spawn(TargetFlyer, Target.Position, map);
        TargetFlyer!.AddThing(Target);

        HauntingActive = true;
    }

    public void LiftPhaseTick()
    {
        if (StartPhaseTick + LiftPhaseTicks < Find.TickManager.TicksGame)
        {
            CurrentPhase++;
            foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
            {
                hauntedThing.Value.Radius = Vector3.Distance(Epicenter, hauntedThing.Key.DrawPos);
                hauntedThing.Value.RotationStart = hauntedThing.Key.DrawPos;
            }
            return;
        }

        float ratio = (float)(Find.TickManager.TicksGame - StartPhaseTick) / LiftPhaseTicks;

        foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        {
            Vector3 EndPos = new(0, 0, hauntedThing.Value.LiftHeight);
            hauntedThing.Key.SetPositionDirectly(Vector3.Lerp(hauntedThing.Key.StartPosition, hauntedThing.Key.StartPosition + EndPos, ratio));
        }

        TargetFlyer.SetPositionDirectly(Vector3.Lerp(StartPos, Epicenter, ratio));
    }

    public void SpinPhaseTick()
    {
        if (StartPhaseTick + LiftPhaseTicks + SpinPhaseTicks < Find.TickManager.TicksGame)
        {
            CurrentPhase++;
            foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
            {
                hauntedThing.Value.EndSpinPosition = hauntedThing.Key.DrawPos;
            }
            return;
        }

        int currentPhaseTick = Find.TickManager.TicksGame - StartPhaseTick;

        foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        {
            hauntedThing.Key.SetPositionDirectly(hauntedThing.Value.CurrentRotatedPos(SpinPhaseTicks, currentPhaseTick));
        }
    }

    public void DropPhaseTick()
    {
        if (StartPhaseTick + LiftPhaseTicks + SpinPhaseTicks + DropPhaseTicks < Find.TickManager.TicksGame)
        {
            HauntingActive = false;
            foreach (HauntedThingFlyer hauntedThingFlyer in HauntedThings.Keys.ToList())
            {
                HauntedThings.Remove(hauntedThingFlyer);
                hauntedThingFlyer.Destroy();
            }

            TargetFlyer.Destroy();
            TargetFlyer = null;

            CurrentPhase = HauntPhase.Lifting;
            LiftPhaseTicks = 0;
            SpinPhaseTicks = 0;
            DropPhaseTicks = 0;
            StartPhaseTick = 0;

            Target = null;
            StartPos = Vector3.zero;

            map.weatherManager.TransitionTo(originalWeather);
            map.weatherManager.curWeatherAge = 3700;

            originalWeather = null;

            mote.Destroy();
            mote = null;

            return;
        }

        float ratio = (float)(Find.TickManager.TicksGame - (StartPhaseTick + LiftPhaseTicks + SpinPhaseTicks)) / DropPhaseTicks;

        foreach (KeyValuePair<HauntedThingFlyer, HauntingParams> hauntedThing in HauntedThings)
        {
            hauntedThing.Key.SetPositionDirectly(Vector3.Lerp(hauntedThing.Value.EndSpinPosition, hauntedThing.Key.StartPosition, ratio));
        }

        TargetFlyer.SetPositionDirectly(Vector3.Lerp(Epicenter, StartPos, ratio));
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

            if (mote != null)
                mote.exactPosition = TargetFlyer.DrawPos;

            if (mote is null or { Destroyed: true })
            {
                mote = MoteMaker.MakeAttachedOverlay(TargetFlyer, DefDatabase<ThingDef>.GetNamed("Mote_PsychicConditionCauserEffect"), Vector3.zero);
            }
            else
            {
                mote.Maintain();
            }
            ticksSinceLastEmitted++;
        }
    }
}
