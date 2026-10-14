using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MSSFP.Hediffs;

public class HiddenHediffWithComps : HediffWithComps
{
    public override bool Visible => true;
}
