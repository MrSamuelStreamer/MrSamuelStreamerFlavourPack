using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompReadable : ThingComp
{
    public CompProperties_Readable Props => (CompProperties_Readable)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }

        Command_Action read = new()
        {
            defaultLabel = Props.buttonLabel,
            defaultDesc = Props.buttonDesc,
            icon = Props.buttonIconTex,
            action = Read,
        };

        yield return read;
    }

    public virtual void Read()
    {
        Find.LetterStack.ReceiveLetter(
            Props.letterLabel.Formatted(),
            Props.letterText.Formatted(),
            Props.letterDef
        );
        TrySpawnThing();
        parent.Destroy();
    }

    public virtual bool TrySpawnThing()
    {
        if (Props.spawnThing == null || Props.spawnCount <= 0) return true;

        IntVec3 result;
        if (!CompSpawner.TryFindSpawnCell(parent, Props.spawnThing, Props.spawnCount, out result))
            return false;
        ThingDef stuff = null;
        if (Props.spawnThing.MadeFromStuff)
        {
            stuff = GenStuff.DefaultStuffFor(Props.spawnThing);
        }
        Thing thing = ThingMaker.MakeThing(Props.spawnThing, stuff);
        thing.stackCount = Props.spawnCount;

        GenPlace.TryPlaceThing(
            thing,
            result,
            parent.Map,
            ThingPlaceMode.Direct,
            out thing
        );

        return true;
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption compFloatMenuOption in base.CompFloatMenuOptions(selPawn))
        {
            yield return compFloatMenuOption;
        }

        yield return new FloatMenuOption(Props.buttonLabel, Read, MenuOptionPriority.High);
    }
}
