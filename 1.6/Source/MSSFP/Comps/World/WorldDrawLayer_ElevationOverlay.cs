using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.World;

public class WorldDrawLayer_ElevationOverlay : WorldDrawLayer
{
    private static readonly Color ColorSeaLevel = Color.green;
    private static readonly Color ColorHighElevation = Color.red;
    private static readonly Color ColorBelowSeaLevel = Color.blue;

    public override bool Visible => MSSFPMod.settings.ShowElevationOverlay;

    public Color GetTileColor(float elev, float minElevation, float maxElevation)
    {
        minElevation = Mathf.Min(minElevation, -1000f);
        maxElevation = Mathf.Max(maxElevation, 1000f);

        Color color;
        if (elev >= 0)
        {
            float t = maxElevation > 0 ? elev / maxElevation : 0;
            color = Color.Lerp(ColorSeaLevel, ColorHighElevation, t);
        }
        else
        {
            float t = minElevation < 0 ? elev / minElevation : 0;
            color = Color.Lerp(ColorSeaLevel, ColorBelowSeaLevel, t);
        }
        color.a = 0.5f;

        return color;
    }
    public override IEnumerable Regenerate()
    {
        foreach (object obj in base.Regenerate())
        {
            yield return obj;
        }

        float minElevation = 0f;
        float maxElevation = 0f;
        RimWorld.Planet.World world = Find.World;
        WorldGrid grid = world.grid;
        int tileCount = grid.TilesCount;

        for (int i = 0; i < tileCount; i++)
        {
            float elev = grid[i].elevation;
            if (elev < minElevation) minElevation = elev;
            if (elev > maxElevation) maxElevation = elev;
        }

        for (int i = 0; i < tileCount; i++)
        {
            Color color = GetTileColor(grid[i].elevation, minElevation, maxElevation);

            List<Vector3> verts = [];
            grid.GetTileVertices(i, verts);

            LayerSubMesh subMesh = GetSubMesh(WorldMaterials.VertexColorTransparent);
            int vertStart = subMesh.verts.Count;

            for (int j = 0; j < verts.Count; j++)
            {
                // Slightly above the terrain to avoid z-fighting
                subMesh.verts.Add(verts[j] + (verts[j].normalized * 0.012f));
                subMesh.colors.Add(color);
                subMesh.uvs.Add(Vector2.zero);
            }

            for (int j = 0; j < verts.Count - 2; j++)
            {
                subMesh.tris.Add(vertStart + j + 2);
                subMesh.tris.Add(vertStart + j + 1);
                subMesh.tris.Add(vertStart);
            }
        }

        FinalizeMesh(MeshParts.All);
    }
}
