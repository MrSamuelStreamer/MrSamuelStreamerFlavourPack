using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

public class Dialog_BedUpgrade(CompUpgradableBed bed, IWindowDrawing customWindowDrawing = null) : Window(customWindowDrawing)
{
    protected readonly CompUpgradableBed Bed = bed;

    public override Vector2 InitialSize => new(620f, 800f);

    private readonly Texture2D BedTex = ContentFinder<Texture2D>.Get("UI/MSSFP_OskarianBed_Double");

    public override void DoWindowContents(Rect inRect)
    {
        RectDivider Outer = new(inRect, 145232335, new Vector2(0f, 0f));

        RectDivider TitleRow = Outer.NewRow(60f, marginOverride: 0f);
        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(TitleRow.Rect.ContractedBy(10f), "Upgrade Bed");
        }

        RectDivider ContentRow = Outer.NewRow(662f, marginOverride: 0f);

        RectDivider BottomButtonRow = Outer.NewRow(42f, marginOverride: 0f);

        RectDivider BottomSpacer = BottomButtonRow.NewCol(BottomButtonRow.Rect.width - 140f, marginOverride: 0f);
        RectDivider BottomButton = BottomButtonRow.NewCol(140f, marginOverride: 0f);
        if (Widgets.ButtonText(BottomButton.Rect.ContractedBy(10f), "Close"))
        {
            Close();
        }

        RectDivider MiddleColumn = ContentRow.NewCol(584f, marginOverride: 0f);

        RectDivider MiddleTop = MiddleColumn.NewRow(400.0f, marginOverride: 0f);
        GUI.DrawTexture(MiddleTop, BedTex, ScaleMode.ScaleToFit);

        RectDivider MiddleBottom = MiddleColumn.NewRow(262.0f, marginOverride: 0f);
        DrawBedStatBox(MiddleBottom);
    }

    public float StatColHeight = 0;
    public Vector2 StatColScrollPosition = Vector2.zero;

    public Color StatRowColor = new(0.1f, 0.1f, 0.1f);
    public Color StatRowColorAlt = new(0.3f, 0.3f, 0.3f);

    public float BedStatBoxColHeight = 0;
    public Vector2 BedStatBoxColScrollPosition = Vector2.zero;

    public void DrawBedStatBox(Rect rect)
    {
        Rect statColContent = new(rect.ContractedBy(10f)) { height = BedStatBoxColHeight, width = rect.width - 16f };

        BedStatBoxColHeight = 0;

        Widgets.BeginScrollView(rect, ref BedStatBoxColScrollPosition, statColContent);

        float width = statColContent.width / 2f;

        using (new TextBlock(GameFont.Small))
        {
            Widgets.Label(statColContent, "Bed Stats    |    Levels: " + Bed.Levels);
            BedStatBoxColHeight += 30f;
        }

        IEnumerator<BedUpgradeDef> enumerator = CompUpgradableBed.BedUpgradesAvailable.GetEnumerator();

        while (enumerator.MoveNext())
        {
            BedUpgradeDef upgrade = enumerator.Current;

            RectDivider statRow = new(
                new Rect(statColContent.x, statColContent.y + BedStatBoxColHeight, statColContent.width, 22f),
                145232335,
                new Vector2(0f, BedStatBoxColHeight)
            );

            RectDivider leftCol = statRow.NewCol(width, marginOverride: 0f);
            RectDivider rightCol = statRow.NewCol(width, marginOverride: 0f);

            RectDivider leftButton = leftCol.NewCol(52, marginOverride: 0f);
            leftCol.NewCol(2, marginOverride: 0f);
            RectDivider leftLabel = leftCol.NewCol(width - 54, marginOverride: 0f);

            RectDivider rightButton = rightCol.NewCol(52, marginOverride: 0f);
            rightCol.NewCol(2, marginOverride: 0f);
            RectDivider rightLabel = rightCol.NewCol(width - 54, marginOverride: 0f);

            if (Widgets.ButtonText(leftButton, upgrade?.Worker.ButtonText(Bed) ?? "+", active: Bed.Levels > 1 && (upgrade?.Worker.CanUpgrade(Bed) ?? false)))
            {
                upgrade?.Worker.DoUpgrade(Bed);
            }
            Widgets.Label(leftLabel, Bed.GetStatString(upgrade));

            if (enumerator.MoveNext())
            {
                upgrade = enumerator.Current;
                if (Widgets.ButtonText(rightButton, upgrade?.Worker.ButtonText(Bed) ?? "+", active: Bed.Levels > 1 && (upgrade?.Worker.CanUpgrade(Bed) ?? false)))
                {
                    upgrade?.Worker.DoUpgrade(Bed);
                }
                Widgets.Label(rightLabel, Bed.GetStatString(enumerator.Current));
            }

            BedStatBoxColHeight += 22f;
        }

        enumerator.Dispose();

        Widgets.EndScrollView();
    }
}
