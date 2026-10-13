using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Draws a HUD list of in-transit <see cref="TunnelCaravan"/>s in the bottom-right of the
/// world view, each row jump-to-selectable on click.
///
/// Gated on <see cref="TunnelUtilities.IsEnabled"/>: the HUD is hidden entirely when
/// tunnels are disabled globally or for the current save.
/// </summary>
[HarmonyPatch(typeof(WorldInterface))]
public static class WorldInterface_Patch
{
    [HarmonyPatch(nameof(WorldInterface.WorldInterfaceOnGUI))]
    [HarmonyPostfix]
    public static void WorldInterfaceOnGUIPostfix()
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (!WorldRendererUtility.WorldSelected || !Find.WorldCamera.gameObject.activeInHierarchy)
            return;
        if (Event.current.type == EventType.Layout)
            return;
        float leftX = UI.screenWidth - 420f;
        float curBaseY = 200f;

        Rect rect = new Rect(leftX, curBaseY, 400, 24);
        GameFont orig = Text.Font;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleRight;

        foreach (TunnelCaravan caravan in Find.WorldObjects.AllWorldObjects.OfType<TunnelCaravan>().Where(c => !c.done && !c.mapGenerating))
        {
            Widgets.Label(rect, caravan.Progress);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                if (Event.current.type == EventType.MouseDown)
                {
                    Event.current.Use();
                    Find.WorldSelector.ClearSelection();
                    Find.WorldSelector.Select(caravan);
                    if (caravan.Tile.Valid)
                    {
                        Find.WorldInterface.SelectedTile = caravan.Tile;
                        Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                    }
                }
            }

            curBaseY += 28;
        }

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = orig;
    }
}
