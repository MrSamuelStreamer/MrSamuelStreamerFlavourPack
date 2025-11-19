using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Letters;

public class DeathLetter : ChoiceLetter
{
    protected DiaOption Option_ReadMore
    {
        get
        {
            GlobalTargetInfo target = lookTargets.TryGetPrimaryTarget();
            DiaOption optionReadMore = new DiaOption("ReadMore".Translate());
            optionReadMore.action = (Action) (() =>
            {
                CameraJumper.TryJumpAndSelect(target);
                Find.LetterStack.RemoveLetter(this);
                InspectPaneUtility.OpenTab(typeof (ITab_Pawn_Log));
            });
            optionReadMore.resolveTree = true;
            if (!target.IsValid)
                optionReadMore.Disable(null);
            return optionReadMore;
        }
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            DeathLetter deathLetter = this;
            yield return deathLetter.Option_Close;
            if (deathLetter.lookTargets.IsValid())
                yield return deathLetter.Option_ReadMore;
            if (deathLetter.quest != null)
                yield return deathLetter.Option_ViewInQuestsTab();
        }
    }

    public override void OpenLetter()
    {
        Pawn targetPawn = lookTargets.TryGetPrimaryTarget().Thing as Pawn;
        TaggedString text = Text;
        string lineList = Find.BattleLog.Battles.Where(battle => battle.Concerns(targetPawn)).SelectMany(battle => battle.Entries.Where(entry => entry.Concerns(targetPawn) && entry.ShowInCompactView())).OrderBy(entry => entry.Age).Take(5).Reverse().Select(entry => "  " + entry.ToGameStringFromPOV(null)).ToLineList();
        if (lineList.Length > 0)
            text = (TaggedString) string.Format("{0}\n\n{1}\n{2}", text, "LastEventsInLife".Translate((NamedArgument) targetPawn.LabelDefinite(), targetPawn.Named("PAWN")).Resolve() + ":", lineList);
        DiaNode nodeRoot = new DiaNode(text);
        nodeRoot.options.AddRange(Choices);
        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, relatedFaction, radioMode: radioMode, title: title));
    }
}
