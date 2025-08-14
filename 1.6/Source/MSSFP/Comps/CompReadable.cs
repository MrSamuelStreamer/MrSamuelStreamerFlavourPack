using System.Collections.Generic;
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
        parent.Destroy();
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
