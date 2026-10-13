using System.Collections.Generic;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

/// <summary>
/// Single-choice picker for the Programmable Healer Mech Serum.
/// Player picks one of: remove an ailment, remove a trait, add a passion.
/// On Confirm: applies the chosen effect and invokes the supplied destroy callback.
/// On Cancel: closes without consuming the serum.
/// </summary>
public class Dialog_ProgrammableHealer : Window
{
    private enum Section { Ailment, Trait, Passion }

    private readonly Pawn pawn;
    private readonly System.Action onConsume;

    private readonly List<Hediff> ailments;
    private readonly List<Trait> traits;
    private readonly List<SkillRecord> passions;

    private Section currentSection = Section.Ailment;
    private int selectedIndex = -1;
    private Vector2 scroll = Vector2.zero;

    public override Vector2 InitialSize => new(720f, 560f);

    public Dialog_ProgrammableHealer(Pawn pawn, System.Action onConsume)
    {
        this.pawn = pawn;
        this.onConsume = onConsume;

        ailments = ProgrammableHealerFilters.RemovableAilments(pawn);
        traits = ProgrammableHealerFilters.RemovableTraits(pawn);
        passions = ProgrammableHealerFilters.UpgradablePassions(pawn);

        doCloseX = true;
        absorbInputAroundWindow = true;
        forcePause = true;
        closeOnClickedOutside = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        float curY = inRect.y;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, curY, inRect.width, 36f),
            $"Programmable Healer — {pawn.LabelShort}");
        Text.Font = GameFont.Small;
        curY += 40f;

        Widgets.Label(new Rect(inRect.x, curY, inRect.width, 24f),
            "Choose a category, then pick one option. Confirm to apply.");
        curY += 28f;

        // Section tabs
        float tabW = inRect.width / 3f;
        DrawTab(new Rect(inRect.x, curY, tabW, 30f), Section.Ailment, $"Remove ailment ({ailments.Count})");
        DrawTab(new Rect(inRect.x + tabW, curY, tabW, 30f), Section.Trait, $"Remove trait ({traits.Count})");
        DrawTab(new Rect(inRect.x + tabW * 2f, curY, tabW, 30f), Section.Passion, $"Add passion ({passions.Count})");
        curY += 34f;

        // Item list
        float listH = inRect.height - (curY - inRect.y) - 60f;
        Rect listOuter = new(inRect.x, curY, inRect.width, listH);
        DrawList(listOuter);
        curY += listH + 10f;

        // Buttons
        Rect cancelBtn = new(inRect.x, curY, 160f, 40f);
        Rect confirmBtn = new(inRect.xMax - 160f, curY, 160f, 40f);
        if (Widgets.ButtonText(cancelBtn, "Cancel"))
        {
            Close();
        }
        bool canConfirm = selectedIndex >= 0 && CurrentListCount() > selectedIndex;
        GUI.color = canConfirm ? Color.white : Color.gray;
        if (Widgets.ButtonText(confirmBtn, "Confirm") && canConfirm)
        {
            ApplyChoice();
            onConsume?.Invoke();
            Close();
        }
        GUI.color = Color.white;
    }

    private void DrawTab(Rect rect, Section section, string label)
    {
        bool selected = currentSection == section;
        if (selected) GUI.color = new Color(0.7f, 0.95f, 0.7f);
        if (Widgets.ButtonText(rect, label))
        {
            currentSection = section;
            selectedIndex = -1;
            scroll = Vector2.zero;
        }
        GUI.color = Color.white;
    }

    private int CurrentListCount() => currentSection switch
    {
        Section.Ailment => ailments.Count,
        Section.Trait => traits.Count,
        Section.Passion => passions.Count,
        _ => 0,
    };

    private void DrawList(Rect outer)
    {
        Widgets.DrawMenuSection(outer);
        int count = CurrentListCount();
        if (count == 0)
        {
            Widgets.Label(outer.ContractedBy(8f),
                "No valid options in this category for this pawn.");
            return;
        }

        const float rowH = 30f;
        Rect viewRect = new(0f, 0f, outer.width - 20f, count * rowH);
        Widgets.BeginScrollView(outer.ContractedBy(4f), ref scroll, viewRect);

        for (int i = 0; i < count; i++)
        {
            Rect row = new(0f, i * rowH, viewRect.width, rowH);
            if (i == selectedIndex) Widgets.DrawHighlightSelected(row);
            else if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

            string label = RowLabel(i);
            Widgets.Label(row.ContractedBy(6f, 0f), label);
            if (Widgets.ButtonInvisible(row)) selectedIndex = i;
        }

        Widgets.EndScrollView();
    }

    private string RowLabel(int i) => currentSection switch
    {
        Section.Ailment => AilmentLabel(ailments[i]),
        Section.Trait => traits[i].LabelCap,
        Section.Passion => $"{passions[i].def.LabelCap} (current: {passions[i].passion}, +1 tier)",
        _ => "?",
    };

    /// <summary>
    /// Renders a hediff with body-part attribution when the hediff is part-scoped
    /// (e.g. "Right leg: Gone"). Whole-body hediffs (malnutrition, addictions) render as plain LabelCap.
    /// </summary>
    private static string AilmentLabel(Hediff h)
    {
        if (h.Part != null)
            return $"{h.Part.LabelCap}: {h.LabelCap}";
        return h.LabelCap;
    }

    private void ApplyChoice()
    {
        switch (currentSection)
        {
            case Section.Ailment:
                Hediff h = ailments[selectedIndex];
                string curedLabel = AilmentLabel(h);
                // For missing parts, RestorePart recurses and clears every Hediff_MissingPart on
                // the part and all its descendants (foot, toes, etc.). Plain RemoveHediff would
                // leave the cascade children behind — a leg-gone pick would heal the leg label
                // but the foot, femur, tibia, and toes would all still be missing.
                if (h is Hediff_MissingPart && h.Part != null)
                    pawn.health.RestorePart(h.Part);
                else
                    pawn.health.RemoveHediff(h);
                Messages.Message($"{pawn.LabelShort}: cured {curedLabel}.", pawn, MessageTypeDefOf.PositiveEvent);
                break;
            case Section.Trait:
                Trait t = traits[selectedIndex];
                pawn.story.traits.RemoveTrait(t);
                Messages.Message($"{pawn.LabelShort}: trait removed — {t.LabelCap}.", pawn, MessageTypeDefOf.PositiveEvent);
                break;
            case Section.Passion:
                SkillRecord s = passions[selectedIndex];
                s.passion = s.passion == Passion.None ? Passion.Minor : Passion.Major;
                Messages.Message($"{pawn.LabelShort}: passion in {s.def.LabelCap} is now {s.passion}.", pawn, MessageTypeDefOf.PositiveEvent);
                break;
        }
    }
}
