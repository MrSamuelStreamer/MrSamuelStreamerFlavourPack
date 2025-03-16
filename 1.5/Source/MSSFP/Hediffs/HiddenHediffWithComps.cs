using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MSSFP.Hediffs;

public class HiddenHediffWithComps : HediffWithComps
{
    public static Lazy<FieldInfo> VisibleInfo = new(() => AccessTools.Field(typeof(HediffWithComps), "visible"));

    public override void PostMake()
    {
        VisibleInfo.Value.SetValue(this, false);
        base.PostMake();
    }
}
