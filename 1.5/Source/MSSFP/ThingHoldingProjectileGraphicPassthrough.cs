using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MSSFP;

public class ThingHoldingProjectileGraphicPassthrough: Graphic
{
    public Thing heldThing;

    public Thing HeldThing
    {
        get => heldThing;
        set => heldThing = value;
    }

    public override Material MatSingle => HeldThing?.Graphic.MatSingle;

    public override Material MatWest => HeldThing?.Graphic.MatWest;

    public override Material MatSouth => HeldThing?.Graphic.MatSouth;

    public override Material MatEast => HeldThing?.Graphic.MatEast;

    public override Material MatNorth => HeldThing?.Graphic.MatNorth;

    public override bool WestFlipped => HeldThing?.Graphic.WestFlipped ?? false;

    public override bool EastFlipped => HeldThing?.Graphic.EastFlipped ?? false;

    public override bool ShouldDrawRotated => HeldThing?.Graphic.ShouldDrawRotated ?? false;

    public override float DrawRotatedExtraAngleOffset => HeldThing?.Graphic.DrawRotatedExtraAngleOffset ?? 0f;

    public override bool UseSameGraphicForGhost => HeldThing?.Graphic.UseSameGraphicForGhost ?? false;

    public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
    {
        HeldThing?.Graphic.TryInsertIntoAtlas(groupKey);
    }

    public override void Init(GraphicRequest req)
    {
        HeldThing?.Graphic.Init(req);
    }

    public override Material NodeGetMat(PawnDrawParms parms)
    {
        return HeldThing?.Graphic.NodeGetMat(parms);
    }

    public override Material MatAt(Rot4 rot, Thing thing = null)
    {
        return HeldThing?.Graphic.MatAt(rot, thing);
    }

    public override Mesh MeshAt(Rot4 rot)
    {
        return HeldThing?.Graphic.MeshAt(rot);
    }

    public override Material MatSingleFor(Thing thing) => HeldThing?.Graphic.MatSingleFor(thing);

    public override void DrawWorker(
      Vector3 loc,
      Rot4 rot,
      ThingDef thingDef,
      Thing thing,
      float extraRotation)
    {
        HeldThing?.Graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }

    public Lazy<MethodInfo> DrawMeshIntAccess = new Lazy<MethodInfo>(()=>AccessTools.Method(typeof(Graphic), "DrawMeshInt"));

    protected override void DrawMeshInt(Mesh mesh, Vector3 loc, Quaternion quat, Material mat)
    {
        DrawMeshIntAccess.Value.Invoke(HeldThing?.Graphic, new object[] { mesh, loc, quat, mat });
    }

    public override void Print(SectionLayer layer, Thing thing, float extraRotation)
    {
        HeldThing?.Graphic.Print(layer, thing, extraRotation);
    }

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return HeldThing?.Graphic.GetColoredVersion(newShader, newColor, newColorTwo);
    }

    [Obsolete("Will be removed in a future release")]
    public override Graphic GetCopy(Vector2 newDrawSize) => HeldThing?.Graphic.GetCopy(newDrawSize);

    public override Graphic GetCopy(Vector2 newDrawSize, Shader overrideShader)
    {
        return HeldThing?.Graphic.GetCopy(newDrawSize, overrideShader);
    }

    public override Graphic GetShadowlessGraphic()
    {
        return HeldThing?.Graphic.GetShadowlessGraphic();
    }
}
