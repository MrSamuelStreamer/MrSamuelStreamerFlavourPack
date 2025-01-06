using System;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public class PawnHistoryEditorWindow(HediffComp_BodyHopHaunt comp) : Window
{
    public HediffComp_BodyHopHaunt hauntComp = comp;

    public override Vector2 InitialSize => new Vector2(800f, 600f);

    public Vector2 scrollPosition = Vector2.zero;

    public HediffComp_BodyHopHaunt.PawnInfo Selected;
    public string buffer;
    public string buffer2;

    public override void DoWindowContents(Rect inRect)
    {
        RectDivider window = new RectDivider(inRect, 1241241241, Vector2.zero);

        RectDivider left = window.NewCol(200f);
        RectDivider right = window.NewCol(600 - 36); // default x margin

        RectDivider newPawnButton = left.NewRow(40f);
        RectDivider pawnListScrollContainer = left.NewRow(660 - 136); // default y margin

        RectDivider title = right.NewRow(60f);
        RectDivider pawnForm = right.NewRow(540 - 36); // default y margin

        #region PawnForm

        Widgets.Label(title.Rect.ContractedBy(10f), "Edit Pawn");

        if (Selected != null)
        {
            RectDivider name = pawnForm.NewRow(60f);
            RectDivider description = pawnForm.NewRow(120f);
            RectDivider bestSkill = pawnForm.NewRow(60f);
            RectDivider skillLevel = pawnForm.NewRow(60f);
            RectDivider traits = pawnForm.NewRow(60f);
            RectDivider tickAdded = pawnForm.NewRow(60f);
            RectDivider save = pawnForm.NewRow(60f);

            RectDivider nameLabel = name.NewCol(100f);
            RectDivider nameEntry = name.NewCol(350f);

            Widgets.Label(nameLabel.Rect.ContractedBy(10f), "Name: ");
            Selected.name = Widgets.TextField(nameEntry.Rect.ContractedBy(10f), Selected.name);

            RectDivider descriptionLabel = description.NewCol(100f);
            RectDivider descriptionEntry = description.NewCol(350f);

            Widgets.Label(descriptionLabel.Rect.ContractedBy(10f), "Description: ");
            Selected.description = Widgets.TextArea(descriptionEntry.Rect.ContractedBy(10f), Selected.description);

            RectDivider skillLabel = bestSkill.NewCol(100f);
            RectDivider skillEntry = bestSkill.NewCol(350f);

            Widgets.Label(skillLabel.Rect.ContractedBy(10f), "Best Skill: ");
            if (Widgets.ButtonText(skillEntry.Rect.ContractedBy(10f), Selected.bestSkill == null ? "Select skill" : Selected.bestSkill.label))
            {
                // Show float
            }

            RectDivider skillLevelLabel = skillLevel.NewCol(100f);
            RectDivider skillLevelEntry = skillLevel.NewCol(350f);

            Widgets.Label(skillLevelLabel.Rect.ContractedBy(10f), "Skill Boost: ");
            Widgets.IntEntry(skillLevelEntry, ref Selected.skillOffset, ref buffer);

            RectDivider traitLabel = traits.NewCol(100f);
            RectDivider traitEntry = traits.NewCol(350f);

            Widgets.Label(traitLabel.Rect.ContractedBy(10f), "Traits: ");
            // if (Widgets.ButtonText(traitEntry.Rect.ContractedBy(10f), Selected.bestSkill == null ? "Select trait" : Selected.bestSkill.label))
            // {
            //     // Show float
            // }

            RectDivider tickAddedLabel = tickAdded.NewCol(100f);
            RectDivider tickAddedEntry = tickAdded.NewCol(350f);

            Widgets.Label(tickAddedLabel.Rect.ContractedBy(10f), "Tick Added: ");
            Widgets.IntEntry(tickAddedEntry, ref Selected.swapTick, ref buffer2);

            if (comp.pawns.Contains(Selected))
            {
                if (Widgets.ButtonText(save, "Close"))
                {
                    Close();
                }
            }
            else
            {
                RectDivider saveRect = save.NewCol(200f);
                RectDivider closeRect = save.NewCol(200f);

                if (Widgets.ButtonText(saveRect, "Save"))
                {
                    comp.pawns.Add(Selected);
                    Selected = null;
                }

                if (Widgets.ButtonText(closeRect, "Close"))
                {
                    Close();
                }
            }
        }

        #endregion

        #region PawnList

        if (Widgets.ButtonText(newPawnButton.Rect.ContractedBy(3f), "New Pawn"))
        {
            // new pawn
            Selected = new HediffComp_BodyHopHaunt.PawnInfo();
        }

        int pawnCount = hauntComp.pawns.Count;

        int pawnListHeight = 100 * pawnCount;

        Rect scrollContainer = pawnListScrollContainer.Rect;
        scrollContainer.xMax -= 30f;
        scrollContainer.yMax -= 30f;

        Rect pawnListRect = new Rect(0,0, scrollContainer.width, pawnListHeight);

        Widgets.BeginScrollView(scrollContainer, ref scrollPosition, pawnListRect);

        try
        {
            int currentHeight = 0;
            foreach (HediffComp_BodyHopHaunt.PawnInfo pawn in hauntComp.pawns)
            {
                Rect Section = new Rect(0, currentHeight, pawnListRect.width, 100f);
                if (Widgets.ButtonText(Section, "", false))
                {
                    Selected = pawn;
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
