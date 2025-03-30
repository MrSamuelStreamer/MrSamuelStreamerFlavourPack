using MSSFP.Comps;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

public class Dialog_Rename(CompUpgradableBed compUpgradableBed, string name, IWindowDrawing customWindowDrawing = null) : Window(customWindowDrawing)
{
    private string _name = name;
    public override Vector2 InitialSize => new(400f, 150f);

    public override void DoWindowContents(Rect inRect)
    {
        closeOnClickedOutside = true;
        RectDivider window = new(inRect, 25267234);

        RectDivider label = window.NewRow(24f);
        Widgets.Label(label, "MSSFP_RenameDialog_Label".Translate());

        RectDivider textArea = window.NewRow(32f);
        _name = Widgets.TextArea(textArea.Rect, _name);

        RectDivider buttonRow = window.NewRow(32f);
        RectDivider sparer = buttonRow.NewCol(100f);
        RectDivider button = buttonRow.NewCol(60f);

        if (Widgets.ButtonText(button.Rect, "MSSFP_RenameDialog_Apply".Translate()))
        {
            compUpgradableBed.NewName = _name;
            Close();
        }
    }
}
