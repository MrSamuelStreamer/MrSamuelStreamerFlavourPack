using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public static class DrawUtils
{
    public static Texture2D MakeReadableTextureInstance(this RenderTexture source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        temporary.name = "MakeReadableTexture_Temp";
        Graphics.Blit(source, temporary);
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = temporary;
        Texture2D texture2D = new(source.width, source.height);
        texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(temporary);
        return texture2D;
    }

    public static Texture2D GetGreyscale(this RenderTexture source)
    {
        Texture2D texture = MakeReadableTextureInstance(source);
        Color[] colors = texture.GetPixels();

        for (int i = 0; i < colors.Length; i++)
        {
            Color c = colors[i];
            float gray = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
            colors[i] = new Color(gray, gray, gray, c.a);
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public static void Print(SectionLayer layer, Pawn pawn, Rot4 rot, Vector3 pos, Graphic graphic, float zoom = 1.5f)
    {
        Vector3 s = new(1.3f, 1f, 1.3f);

        pos += graphic.DrawOffset(rot);

        // Pass in PawnHealthState.Mobile as an override to ensure the pawn is drawn upright
        RenderTexture texture = PortraitsCache.Get(pawn, new Vector2(175f, 175f), rot, new Vector3(0f, 0f, 0.1f), zoom, healthStateOverride: PawnHealthState.Mobile);

        MaterialRequest req2 = default;
        req2.mainTex = texture.GetGreyscale();

        req2.shader = graphic.data?.shaderType?.Shader;
        if (req2.shader == null)
            req2.shader = ShaderDatabase.DefaultShader;

        Mesh mesh = Object.Instantiate(graphic.MeshAt(rot));
        Material mat = MaterialPool.MatFrom(req2);

        //Somehow this magically fixes the flipping issue, just keeping it this way.
        // mesh.SetUVs(false);
        Printer_Mesh.PrintMesh(layer, Matrix4x4.TRS(pos, rot.AsQuat, s), mesh, mat);
    }
}
