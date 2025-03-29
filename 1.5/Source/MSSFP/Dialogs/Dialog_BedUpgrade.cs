using MSSFP.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

public class Dialog_BedUpgrade(CompUpgradableBed bed, IWindowDrawing customWindowDrawing = null) : Window(customWindowDrawing)
{
    protected readonly CompUpgradableBed Bed = bed;

    public override Vector2 InitialSize => new Vector2(1000f, 800f);

    private readonly Texture2D BedTex = ContentFinder<Texture2D>.Get("UI/MSSFP_OskarianBed_Double");

    public override void DoWindowContents(Rect inRect)
    {
        using (TextBlock.Default())
        {
            RectDivider Outer = new(inRect, 145232335, new Vector2(0f, 0f));

            RectDivider TitleRow = Outer.NewRow(42f, marginOverride: 0f);
            Widgets.DrawRectFast(TitleRow.Rect, new Color(1f, 0f, 0f));

            Widgets.Label(TitleRow.Rect.ContractedBy(10f), "Upgrade Bed");

            RectDivider ContentRow = Outer.NewRow(680f, marginOverride: 0f);

            RectDivider BottomButtonRow = Outer.NewRow(42f, marginOverride: 0f);
            Widgets.DrawRectFast(BottomButtonRow.Rect, new Color(1, 1f, 1f));

            RectDivider LeftColumn = ContentRow.NewCol(200f, marginOverride: 0f);
            Widgets.DrawRectFast(LeftColumn.Rect, new Color(0, 1f, 0f));

            RectDivider MiddleColumn = ContentRow.NewCol(600f, marginOverride: 0f);

            RectDivider RightColumn = ContentRow.NewCol(200f, marginOverride: 0f);
            DrawStatColumn(RightColumn);

            RectDivider MiddleTop = MiddleColumn.NewRow(140.0f, marginOverride: 0f);
            Widgets.DrawRectFast(MiddleTop.Rect, new Color(1, 0f, 1f));

            RectDivider MiddleMiddle = MiddleColumn.NewRow(400.0f, marginOverride: 0f);
            GUI.DrawTexture(MiddleMiddle, BedTex, ScaleMode.ScaleToFit);

            RectDivider MiddleBottom = MiddleColumn.NewRow(140.0f, marginOverride: 0f);
            Widgets.DrawRectFast(MiddleBottom.Rect, new Color(0, 1f, 1f));
        }
    }

    public float StatColHeight = 0;
    public Vector2 StatColScrollPosition = Vector2.zero;

    public Color StatRowColor = new(0.1f, 0.1f, 0.1f);
    public Color StatRowColorAlt = new(0.3f, 0.3f, 0.3f);

    public void DrawStatColumn(RectDivider col)
    {
        Rect statColContent = col.Rect;
        statColContent.height = StatColHeight;
        statColContent.width = statColContent.width - 16f;

        StatColHeight = 0;

        Widgets.BeginScrollView(col, ref StatColScrollPosition, statColContent);
        // bool alt = false;

        foreach (StatDef stat in CompUpgradableBed.StatDefs)
        {
            Rect statrow = new Rect(0, StatColHeight, statColContent.width, 60f);
            // Widgets.DrawRectFast(statrow, alt ? StatRowColorAlt : StatRowColor);
            // alt = !alt;

            Widgets.Label(statrow, stat.LabelCap);
            StatColHeight += 60f;
        }

        Widgets.EndScrollView();
    }
}
