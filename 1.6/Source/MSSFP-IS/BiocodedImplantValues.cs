using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.IS;

/// <summary>
/// Bakes an explicit MarketValue statBase onto every generated variant.
///
/// This CANNOT be done during def generation. Most implant ThingDefs carry no MarketValue
/// statBase at all — vanilla `BionicArm` (Core/Defs/HediffDefs/BodyParts/
/// Hediffs_BodyParts_Bionic.xml) lists only Mass, and its value is derived from `costList`
/// by `StatWorker_MarketValue.CalculatedBaseMarketValue`. Since the variant deliberately
/// strips `costList`, an unbaked variant would price at roughly zero — reproducing exactly
/// the `StatPart_Biocoded` behaviour this whole design exists to avoid.
///
/// `BaseMarketValue` is not derivable until references resolve, hence
/// `[StaticConstructorOnStartup]` rather than the generation hook.
/// </summary>
[StaticConstructorOnStartup]
public static class BiocodedImplantValues
{
    static BiocodedImplantValues()
    {
        foreach (KeyValuePair<ThingDef, ThingDef> pair in BiocodedImplantDefs.Map)
        {
            float value = pair.Key.BaseMarketValue * BiocodedImplantDefs.ValueFraction;
            SetStatBase(pair.Value, StatDefOf.MarketValue, value);
        }
    }

    private static void SetStatBase(ThingDef def, StatDef stat, float value)
    {
        def.statBases ??= new List<StatModifier>();

        for (int i = 0; i < def.statBases.Count; i++)
        {
            if (def.statBases[i].stat == stat)
            {
                def.statBases[i].value = value;
                return;
            }
        }

        def.statBases.Add(new StatModifier { stat = stat, value = value });
    }
}
