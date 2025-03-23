using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dryad;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Dryads.Comps;

public class CompAbilityFungalConversion : CompAbilityEffect
{
    public static Lazy<FieldInfo> plants = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(CompNewTreeConnection), "plants"));

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        FleckMaker.Static(target.Cell, parent.pawn.Map, MSSFPDefOf.PsycastPsychicEffect);

        foreach (Thing current in target.Cell.GetThingList(parent.pawn.Map))
        {
            if (current is not Plant plantTarget || plantTarget.def.plant == null || !plantTarget.def.plant.IsTree || plantTarget.def.defName != "Plant_TreeGauranlen")
                continue;

            CompTreeConnection connection = plantTarget.GetComp<CompTreeConnection>();
            if (connection != null)
            {
                plantTarget.AllComps.Remove(connection);
            }

            Plant newTree = (Plant)GenSpawn.Spawn(MSSFPDryadDefOf.MSSFP_Plant_TreeFroganlen, plantTarget.Position, plantTarget.Map, WipeMode.Vanish);
            newTree.Growth = plantTarget.Growth;
            plantTarget.Destroy();

            if (connection != null)
            {
                newTree.AllComps.RemoveWhere(comp => comp is CompTreeConnection);
                newTree.AllComps.Add(connection);
                connection.parent = newTree;

                if (connection is CompNewTreeConnection newConnection)
                {
                    List<Thing> connectedPlants = (List<Thing>)plants.Value.GetValue(newConnection);
                    if (!connectedPlants.NullOrEmpty())
                    {
                        foreach (CompGauranlenConnection comp in connectedPlants.OfType<ThingWithComps>().Select(p => p.GetComp<CompGauranlenConnection>()))
                        {
                            comp.parentTree = newTree;
                        }
                    }
                }
            }
        }
    }
}
