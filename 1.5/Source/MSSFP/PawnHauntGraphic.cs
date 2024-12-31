using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace MSSFP;

public class PawnHauntGraphic : Graphic_Multi
{
    public static Lazy<FieldInfo> matsInfo = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(PawnHauntGraphic), "mats"));
    public Material[] _mats;
    public float zoom = 0.9f;
    protected Shader _shader;
    protected int _renderQueue;
    protected List<ShaderParameter> _shaderParameters;
    protected Pawn _pawn;

    public Material overrideMat;

    public override void Init(GraphicRequest req)
    {
        data = req.graphicData;
        path = req.path;
        maskPath = req.maskPath;
        color = req.color;
        colorTwo = req.colorTwo;
        drawSize = req.drawSize;
        _shader = req.shader;
        _renderQueue = req.renderQueue;
        _shaderParameters = req.shaderParameters;
        _mats = matsInfo.Value.GetValue(this) as Material[];
    }

    public void SetMaterial(Pawn pawn, Rot4 rot)
    {
        MaterialRequest req1 = new()
        {
            mainTex = PortraitsCache.Get(pawn, new Vector2(175f, 175f), Rot4.South, new Vector3(0f, 0f, 0.1f), zoom, healthStateOverride: PawnHealthState.Mobile),
            shader = _shader,
            color = color,
            colorTwo = colorTwo,
            renderQueue = _renderQueue,
            shaderParameters = _shaderParameters
        };

        _mats[rot.AsInt] = MaterialPool.MatFrom(req1);

        _pawn = pawn;
    }

    public void SetOverrideMaterial(Texture2D tex)
    {
        MaterialRequest req1 = new()
        {
            mainTex = tex,
            shader = _shader,
            color = color,
            colorTwo = colorTwo,
            renderQueue = _renderQueue,
            shaderParameters = _shaderParameters
        };

        overrideMat = MaterialPool.MatFrom(req1);
    }

    public override Material MatAt(Rot4 rot, Thing thing = null)
    {
        if (overrideMat != null) return overrideMat;
        if (thing is not Pawn pawn) return BaseContent.BadMat;

        if (_mats[rot.AsInt] is null) SetMaterial(pawn, rot);
        if (pawn != _pawn) SetMaterial(pawn, rot);

        return _mats[rot.AsInt];
    }


}
