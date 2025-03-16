using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MSSFP.Animation;

public class AnimationContext : IExposable, ILoadReferenceable
{
    protected AnimatedThingHolder _thingHolder;

    public AnimationContext(AnimatedThingHolder thingHolder)
    {
        _thingHolder = thingHolder;
        InitialPosition = thingHolder.InnerThing.DrawPos;
        Position = thingHolder.InnerThing.DrawPos;
    }

    public int Id;
    public Dictionary<string, string> Strings = new();
    public Dictionary<string, int> Integers = new();
    public Dictionary<string, float> Floats = new();

    public AnimatedThingHolder Parent => _thingHolder;

    public Vector3 InitialPosition;

    public Vector3 Position;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref Strings, "Strings", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref Integers, "Integers", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref Floats, "Floats", LookMode.Value, LookMode.Value);
        Scribe_Values.Look(ref Position, "Position", Vector3.zero);
        Scribe_Values.Look(ref InitialPosition, "InitialPosition", Vector3.zero);
        Scribe_Values.Look(ref Id, "Id", -1);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (Id == -1)
                Id = Find.UniqueIDsManager.GetNextAbilityID();
        }
    }

    public string GetUniqueLoadID() => "MSSFP_AnimationContext_" + Id;

    public int GetOrParseInt(string key)
    {
        return Integers.TryGetValue(key, out int value) ? value : int.Parse(key);
    }

    public float GetOrParseFloat(string key)
    {
        return Floats.TryGetValue(key, out float value) ? value : float.Parse(key);
    }

    public bool TryGetOrParseInt(string key, out int value)
    {
        return Integers.TryGetValue(key, out value) || int.TryParse(key, out value);
    }

    public bool TryGetOrParseFloat(string key, out float value)
    {
        return Floats.TryGetValue(key, out value) || float.TryParse(key, out value);
    }
}
