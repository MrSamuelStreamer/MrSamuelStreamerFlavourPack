using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.World;

public class ElevationOverlayWorldComponent : WorldComponent
{
    private bool cachedShowElevationOverlay;

    public ElevationOverlayWorldComponent(RimWorld.Planet.World world) : base(world)
    {
        cachedShowElevationOverlay = MSSFPMod.settings.ShowElevationOverlay;
    }

    public override void WorldComponentUpdate()
    {
        base.WorldComponentUpdate();
        // set dirty when the setting is toggled.
        if (cachedShowElevationOverlay != MSSFPMod.settings.ShowElevationOverlay)
        {
            cachedShowElevationOverlay = MSSFPMod.settings.ShowElevationOverlay;
            Find.World.renderer.GetLayer<WorldDrawLayer_ElevationOverlay>(Find.WorldGrid.Surface).SetDirty();
        }
    }

    public override void WorldComponentOnGUI()
    {
        base.WorldComponentOnGUI();

        if (MSSFPMod.settings.ShowElevationOverlay)
        {
            int tile = GenWorld.MouseTile();
            if (tile >= 0)
            {
                float elev = Find.WorldGrid[tile].elevation;
                string label = "MSSFP_Elevation".Translate(elev.ToString("F0"));
                Vector2 mousePos = Event.current.mousePosition;
                Text.Font = GameFont.Small;
                Vector2 size = Text.CalcSize(label);
                Rect rect = new Rect(mousePos.x + 15f, mousePos.y + 15f, size.x + 10f, size.y + 4f);
                AbsTickWindowRect(ref rect); // Adjust if it goes off screen
                Widgets.DrawWindowBackground(rect);
                Widgets.Label(rect.ContractedBy(5f, 2f), label);
            }
        }
    }

    private static void AbsTickWindowRect(ref Rect rect)
    {
        if (rect.xMax > (float)UI.screenWidth)
        {
            rect.x -= rect.xMax - (float)UI.screenWidth;
        }

        if (rect.yMax > (float)UI.screenHeight)
        {
            rect.y -= rect.yMax - (float)UI.screenHeight;
        }

        if (rect.x < 0f)
        {
            rect.x = 0f;
        }

        if (rect.y < 0f)
        {
            rect.y = 0f;
        }
    }
}
