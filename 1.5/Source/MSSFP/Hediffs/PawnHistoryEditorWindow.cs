using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class PawnHistoryEditorWindow(HediffComp_BodyHopHaunt comp) : Window
{
    public HediffComp_BodyHopHaunt hauntComp = comp;

    public override Vector2 InitialSize => new Vector2(1000f, 800f);

    public Vector2 scrollPosition = Vector2.zero;
    public Vector2 traitScrollPosition = Vector2.zero;

    public HediffComp_BodyHopHaunt.PawnInfo Selected;
    public string buffer;
    public string buffer2;

    public override void DoWindowContents(Rect inRect)
    {
        #region RectDivider Setup
        RectDivider window = new RectDivider(inRect, 1241241241, Vector2.zero);

        RectDivider left = window.NewCol(200f);
        RectDivider right = window.NewCol(800 - 36); // default x margin

        RectDivider newPawnButton = left.NewRow(40f);
        RectDivider pawnListScrollContainer = left.NewRow(660 - 136); // default y margin

        RectDivider title = right.NewRow(60f);
        RectDivider pawnForm = right.NewRow(740 - 36); // default y margin

        RectDivider name = pawnForm.NewRow(40f);
        RectDivider nameLabel = name.NewCol(100f);
        RectDivider nameEntry = name.NewCol(550f);

        RectDivider description = pawnForm.NewRow(120f);
        RectDivider descriptionLabel = description.NewCol(100f);
        RectDivider descriptionEntry = description.NewCol(550f);

        RectDivider bestSkill = pawnForm.NewRow(40f);
        RectDivider skillLabel = bestSkill.NewCol(100f);
        RectDivider skillEntry = bestSkill.NewCol(550f);

        RectDivider skillLevel = pawnForm.NewRow(40f);
        RectDivider skillLevelLabel = skillLevel.NewCol(100f);
        RectDivider skillLevelEntry = skillLevel.NewCol(550f);

        RectDivider traits = pawnForm.NewRow(60f);
        RectDivider traitLabel = traits.NewCol(100f);
        RectDivider traitEntry = traits.NewCol(550f);

        RectDivider removeTraits = pawnForm.NewRow(184f);

        RectDivider tickAdded = pawnForm.NewRow(60f);
        RectDivider tickAddedLabel = tickAdded.NewCol(100f);
        RectDivider tickAddedEntry = tickAdded.NewCol(550f);

        RectDivider save = pawnForm.NewRow(60f);
        #endregion

        #region PawnForm

        Widgets.Label(title.Rect.ContractedBy(10f), "Edit Pawn");

        if (Selected != null)
        {
            Widgets.Label(nameLabel.Rect.ContractedBy(10f), "Name: ");
            Selected.name = Widgets.TextField(nameEntry.Rect.ContractedBy(4f), Selected.name);

            Widgets.Label(descriptionLabel.Rect.ContractedBy(10f), "Description: ");
            Selected.description = Widgets.TextArea(descriptionEntry.Rect.ContractedBy(4f), Selected.description);

            Widgets.Label(skillLabel.Rect.ContractedBy(10f), "Best Skill: ");
            if (Widgets.ButtonText(skillEntry.Rect.ContractedBy(4f), Selected.bestSkill == null ? "Select skill" : Selected.bestSkill.label))
            {
                // Show float
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefsListForReading)
                {
                    FloatMenuOption opt = new FloatMenuOption(
                        skillDef.skillLabel,
                        () =>
                        {
                            Selected.bestSkill = skillDef;
                        }
                    );
                    options.Add(opt);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            Widgets.Label(skillLevelLabel.Rect.ContractedBy(10f), "Skill Boost: ");
            Widgets.IntEntry(skillLevelEntry.Rect.ContractedBy(4f), ref Selected.skillOffset, ref buffer);

            Widgets.Label(traitLabel.Rect.ContractedBy(10f), "Traits: ");
            if (Widgets.ButtonText(traitEntry.Rect.ContractedBy(4f), "Select trait"))
            {
                if (Selected.passedTraits == null)
                    Selected.passedTraits = new List<TraitDef>();

                List<FloatMenuOption> options = new List<FloatMenuOption>();
                IEnumerable<TraitDef> validTraits = DefDatabase<TraitDef>
                    .AllDefsListForReading.Except(Selected.passedTraits)
                    .Where(td => !Selected.passedTraits.Any(t => t.ConflictsWith(td)))
                    .Where(td => td.conflictingPassions.NullOrEmpty());

                foreach (TraitDef traitDef in validTraits)
                {
                    FloatMenuOption opt = new FloatMenuOption(
                        traitDef.defName,
                        () =>
                        {
                            Selected.passedTraits.Add(traitDef);
                        }
                    );
                    options.Add(opt);
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (!Selected.passedTraits.NullOrEmpty())
            {
                try
                {
                    Rect traitScrollContainer = removeTraits.Rect;
                    traitScrollContainer.yMax -= 30;
                    traitScrollContainer.xMax -= 30;

                    int scrollViewHeight = Selected.passedTraits.Count * 45;
                    Rect scrollViewRect = new Rect(0f, 0f, traitScrollContainer.width, scrollViewHeight);
                    Widgets.BeginScrollView(traitScrollContainer, ref traitScrollPosition, scrollViewRect);

                    int currentHeight = 0;
                    TraitDef toRemove = null;

                    foreach (TraitDef traitDef in Selected.passedTraits)
                    {
                        if (Widgets.ButtonText(new Rect(0, currentHeight, 300f, 40), $"Remove: {traitDef.defName}"))
                        {
                            toRemove = traitDef;
                        }

                        currentHeight += 45;
                    }

                    if (toRemove != null)
                        Selected.passedTraits.Remove(toRemove);
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }

            Widgets.Label(tickAddedLabel.Rect.ContractedBy(10f), "Tick Added: ");
            Widgets.IntEntry(tickAddedEntry.Rect.ContractedBy(4f), ref Selected.swapTick, ref buffer2);

            RectDivider saveRect = save.NewCol(200f);
            RectDivider closeRect = save.NewCol(200f);

            if (Selected != null)
            {
                if (Widgets.ButtonText(saveRect, "Save"))
                {
                    hauntComp.AddNewPawn(Selected);
                    Selected = null;
                    buffer = null;
                    buffer2 = null;
                }
            }

            if (Widgets.ButtonText(closeRect, "Close"))
            {
                Close();
            }
        }

        #endregion

        #region PawnList

        if (Widgets.ButtonText(newPawnButton.Rect.ContractedBy(3f), "New Pawn"))
        {
            // new pawn
            Selected = new HediffComp_BodyHopHaunt.PawnInfo();
            buffer = null;
            buffer2 = null;
        }

        int pawnCount = hauntComp.pawns.Count;

        int pawnListHeight = 100 * pawnCount;

        Rect scrollContainer = pawnListScrollContainer.Rect;
        scrollContainer.xMax -= 30f;
        scrollContainer.yMax -= 30f;

        Rect pawnListRect = new Rect(0, 0, scrollContainer.width, pawnListHeight);

        Widgets.BeginScrollView(scrollContainer, ref scrollPosition, pawnListRect);

        try
        {
            int currentHeight = 0;
            foreach (HediffComp_BodyHopHaunt.PawnInfo pawn in hauntComp.pawns)
            {
                Rect Section = new Rect(0, currentHeight, pawnListRect.width, 100f);
                if (Widgets.ButtonText(Section, "", false))
                {
                    Selected = pawn.Copy();
                    buffer = null;
                    buffer2 = null;
                }

                if (Selected == pawn)
                {
                    Widgets.DrawRectFast(Section, Color.grey);
                }

                Rect label = new Rect(0, currentHeight, pawnListRect.width, 20f);
                Widgets.Label(label, pawn.name);

                Widgets.DrawLineHorizontal(5, currentHeight + 90f, 90);
                currentHeight += 100;
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }

        #endregion
    }
}
