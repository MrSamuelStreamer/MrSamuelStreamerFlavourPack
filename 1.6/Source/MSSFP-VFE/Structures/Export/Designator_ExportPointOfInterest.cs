using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.VFE.Structures.Export;

/// <summary>
/// Architect &gt; Orders designator, dev mode only, that opens the point-of-interest export dialog for
/// a dragged rectangle. Sits beside KCSG's own export designators. Registered by a patch.
/// </summary>
public class Designator_ExportPointOfInterest : Designator
{
    public override bool Visible => DebugSettings.ShowDevGizmos;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public Designator_ExportPointOfInterest()
    {
        defaultLabel = "Export POI";
        defaultDesc = "Export the selected area as a point-of-interest structure, with pawns and corpses, to one XML file on the Desktop.";
        // Reuse KCSG's export icon; null just leaves the button without one.
        icon = ContentFinder<Texture2D>.Get("UI/Designators/export", false);
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
    }

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c) => c.InBounds(Map);

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        Find.WindowStack.Add(new Dialog_ExportPointOfInterest(Map, cells.ToList()));
    }
}
