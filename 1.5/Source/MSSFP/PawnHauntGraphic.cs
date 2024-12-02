using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class PawnHauntGraphic: Graphic
{
    protected Material _material;
    public float zoom = 1.5f;

    public override void Init(GraphicRequest req)
    {
        data = req.graphicData;
        path = req.path;
        maskPath = req.maskPath;
        color = req.color;
        colorTwo = req.colorTwo;
        drawSize = req.drawSize;
    }

    public override Material MatAt(Rot4 rot, Thing thing = null)
    {
        if(thing is not Pawn pawn) return BaseContent.BadMat;

        if (_material == null)
        {
            Vector3 s = new(1.3f, 1f, 1.3f);

            Vector3 pos = DrawOffset(rot);

            // Pass in PawnHealthState.Mobile as an override to ensure the pawn is drawn upright
            RenderTexture texture = PortraitsCache.Get(pawn, new Vector2(175f, 175f), rot, new Vector3(0f, 0f, 0.1f), zoom, healthStateOverride: PawnHealthState.Mobile);

            MaterialRequest req2 = default;
            req2.mainTex = texture.GetGreyscale();

            req2.shader = data?.shaderType?.Shader;
            if (req2.shader == null) req2.shader = ShaderDatabase.DefaultShader;

            _material = MaterialPool.MatFrom(req2);
        }

        return _material;
    }

}
