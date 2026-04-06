using System.IO;
using System.Linq;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;
using PawnGraphicUtils = MSSFP.Utils.PawnGraphicUtils;

namespace MSSFP.Tabs;

/// <summary>
/// Inspector tab showing the full echo history for a pawn carrying
/// a HediffComp_Echo. Renders each PawnInfo entry with portrait,
/// name, skill boost, traits, and date of hop.
/// </summary>
public class ITab_Echo : ITab
{
    private static readonly Vector2 WinSize = new Vector2(450f, 480f);
    private const float EntryHeight = 80f;
    private const float EntrySpacing = 4f;
    private const float PortraitWidth = 68f;

    private Vector2 scrollPos;

    public ITab_Echo()
    {
        size = WinSize;
        labelKey = "MSS_FP_ITab_Echo";
    }

    private new Pawn SelPawn => SelThing as Pawn;

    public override bool IsVisible
    {
        get
        {
            Pawn pawn = SelPawn;
            return pawn != null && FindEchoComp(pawn)?.pawns.Count > 0;
        }
    }

    protected override void FillTab()
    {
        Pawn pawn = SelPawn;
        if (pawn == null)
            return;

        HediffComp_Echo comp = FindEchoComp(pawn);
        if (comp == null)
            return;

        Rect outerRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        float totalHeight = comp.pawns.Count * (EntryHeight + EntrySpacing);
        Rect viewRect = new Rect(0f, 0f, outerRect.width - 16f, totalHeight);

        Widgets.BeginScrollView(outerRect, ref scrollPos, viewRect);

        float y = 0f;
        foreach (HediffComp_Echo.PawnInfo info in comp.pawns)
        {
            DrawEntry(info, comp, viewRect.width, y);
            y += EntryHeight + EntrySpacing;
        }

        Widgets.EndScrollView();
    }

    private static void DrawEntry(
        HediffComp_Echo.PawnInfo info,
        HediffComp_Echo comp,
        float width,
        float y
    )
    {
        Rect row = new Rect(0f, y, width, EntryHeight);
        Widgets.DrawMenuSection(row);

        // Portrait
        Rect portraitRect = new Rect(4f, y + 4f, PortraitWidth, EntryHeight - 8f);
        Texture2D tex = ResolvePortrait(info, comp);
        if (tex != null)
            GUI.DrawTexture(portraitRect, tex, ScaleMode.ScaleToFit);
        else
            Widgets.DrawBoxSolid(portraitRect, new Color(0.2f, 0.2f, 0.2f));

        // Text block
        float tx = portraitRect.xMax + 8f;
        float tw = width - tx - 4f;

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(tx, y + 4f, tw, 20f), info.name ?? "Unknown");

        if (info.bestSkill != null)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(
                new Rect(tx, y + 26f, tw, 18f),
                $"{info.bestSkill.skillLabel}: +{info.skillOffset}"
            );
        }

        if (!info.passedTraits.NullOrEmpty())
        {
            Text.Font = GameFont.Tiny;
            string traitNames = string.Join(
                ", ",
                info.passedTraits.Select(t =>
                    t.degreeDatas?.FirstOrDefault()?.label ?? t.defName
                )
            );
            Widgets.Label(new Rect(tx, y + 44f, tw, 18f), traitNames);
        }

        if (info.swapTick > 0)
        {
            Text.Font = GameFont.Tiny;
            int tile = Find.CurrentMap?.Tile ?? 0;
            Vector2 longLat = Find.WorldGrid.LongLatOf(tile);
            int year = GenDate.Year((long)info.swapTick, longLat.x);
            int dayOfSeason = GenDate.DayOfSeason((long)info.swapTick, longLat.x) + 1;
            string season = GenDate.Season((long)info.swapTick, longLat).LabelCap();
            string dateStr = $"{dayOfSeason} of {season}, {year}";
            Widgets.Label(new Rect(tx, y + EntryHeight - 22f, tw, 18f), dateStr);
        }

        Text.Font = GameFont.Small;
    }

    private static Texture2D ResolvePortrait(
        HediffComp_Echo.PawnInfo info,
        HediffComp_Echo comp
    )
    {
        if (comp.pawnTextureCache.TryGetValue(info, out Texture2D cached))
            return cached;
        if (info.texPath == null)
            return null;

        Texture2D tex = PawnGraphicUtils.LoadTexture(
            Path.Combine(PawnGraphicUtils.SaveDataPath, info.texPath)
        );
        comp.pawnTextureCache[info] = tex;
        return tex;
    }

    private static HediffComp_Echo FindEchoComp(Pawn pawn)
    {
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is not HediffWithComps hwc)
                continue;
            foreach (HediffComp comp in hwc.comps)
            {
                if (comp is HediffComp_Echo echo)
                    return echo;
            }
        }
        return null;
    }
}
