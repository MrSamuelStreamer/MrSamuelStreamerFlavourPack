using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Letters;

public class AuthorLetter : ChoiceLetter
{
    protected DiaOption Option_ReadMore
    {
        get
        {
            DiaOption optionReadMore = new DiaOption("ReadMore".Translate());
            optionReadMore.action = (Action) (() =>
            {
                Find.LetterStack.RemoveLetter(this);
                InspectPaneUtility.OpenTab(typeof (ITab_Pawn_Log));
            });
            optionReadMore.resolveTree = true;

            return optionReadMore;
        }
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            AuthorLetter authorLetter = this;
            yield return authorLetter.Option_Close;
            if (authorLetter.lookTargets.IsValid())
                yield return authorLetter.Option_ReadMore;
        }
    }

    public override void OpenLetter()
    {
        DiaNode nodeRoot = new DiaNode(Text);
        nodeRoot.options.AddRange(Choices);
        Find.WindowStack.Add(new Dialog_NodeTree(nodeRoot, radioMode: radioMode, title: title));
    }
}
