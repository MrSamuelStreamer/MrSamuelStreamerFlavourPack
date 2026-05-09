using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))]
internal static class TimeControls_SubNormalSpeed_Patch
{
    private static readonly SlowLevel[] LevelsLeftToRight =
    {
        SlowLevel.Eighth,
        SlowLevel.Quarter,
        SlowLevel.Half,
    };

    // Cross-patch state. TickManager_RecordSpeedSetter_Patch reads/writes these
    // to flag any CurTimeSpeed setter call that vanilla makes inside the
    // DoTimeControlsGUI body (i.e. user-driven button/keybind input).
    internal static bool InTimeControlsGUI;
    internal static bool SetterFiredDuringGUI;
    internal static TimeSpeed SetterValue;

    [HarmonyPrefix]
    private static void Prefix()
    {
        InTimeControlsGUI = true;
        SetterFiredDuringGUI = false;
    }

    [HarmonyPostfix]
    private static void Postfix(Rect timerRect)
    {
        bool vanillaSetter = SetterFiredDuringGUI;
        TimeSpeed vanillaSetValue = SetterValue;
        // Turn the flag off BEFORE any of our own setter calls below — those
        // must not be mistaken for user-driven vanilla input.
        InTimeControlsGUI = false;

        if (!MSSFPMod.settings.EnableSubNormalSpeeds)
            return;

        TickManager tickManager = Find.TickManager;
        SubNormalSpeedComponent comp = SubNormalSpeedComponent.Current;
        if (comp == null)
            return;

        if (comp.CurrentLevel != SlowLevel.None && vanillaSetter
            && vanillaSetValue != TimeSpeed.Paused)
        {
            comp.CurrentLevel = SlowLevel.None;
        }

        DrawSlowButtons(comp, tickManager, timerRect);

        if (Event.current.type != EventType.KeyDown)
            return;
        if (Find.WindowStack.WindowsForcePause)
            return;

        if (MSSFPDefOf.MSSFP_TimeSpeed_Half.KeyDownEvent)
        {
            Activate(comp, tickManager, SlowLevel.Half);
            Event.current.Use();
        }
        else if (MSSFPDefOf.MSSFP_TimeSpeed_Quarter.KeyDownEvent)
        {
            Activate(comp, tickManager, SlowLevel.Quarter);
            Event.current.Use();
        }
        else if (MSSFPDefOf.MSSFP_TimeSpeed_Eighth.KeyDownEvent)
        {
            Activate(comp, tickManager, SlowLevel.Eighth);
            Event.current.Use();
        }
    }

    private static void DrawSlowButtons(SubNormalSpeedComponent comp, TickManager tickManager, Rect timerRect)
    {
        Vector2 buttonSize = TimeControls.TimeButSize;
        float rowWidth = buttonSize.x * LevelsLeftToRight.Length;
        // Draw to the LEFT of the vanilla speed bar in the same row. Postfix
        // runs after vanilla's Widgets.EndGroup, so absolute coords are fine.
        Rect rowRect = new Rect(
            timerRect.x - rowWidth,
            timerRect.y,
            rowWidth,
            buttonSize.y
        );

        Widgets.BeginGroup(rowRect);
        Rect rect = new Rect(0f, 0f, buttonSize.x, buttonSize.y);
        foreach (SlowLevel level in LevelsLeftToRight)
        {
            Texture2D icon = TextureFor(level);
            string tooltip = TooltipFor(level);
            if (Widgets.ButtonImage(rect, icon, doMouseoverSound: true, tooltip) && !tickManager.ForcePaused)
            {
                Activate(comp, tickManager, level);
            }
            if (comp.CurrentLevel == level && tickManager.CurTimeSpeed == TimeSpeed.Normal && !tickManager.ForcePaused)
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            rect.x += rect.width;
        }
        Widgets.EndGroup();
        GenUI.AbsorbClicksInRect(rowRect);
    }

    private static void Activate(SubNormalSpeedComponent comp, TickManager tickManager, SlowLevel level)
    {
        comp.CurrentLevel = level;
        if (tickManager.CurTimeSpeed != TimeSpeed.Normal)
            tickManager.CurTimeSpeed = TimeSpeed.Normal;
        SoundDefOf.Clock_Normal.PlayOneShotOnCamera();
        PlayerKnowledgeDatabase.KnowledgeDemonstrated(
            ConceptDefOf.TimeControls,
            KnowledgeAmount.SpecificInteraction
        );
    }

    private static Texture2D TextureFor(SlowLevel level) =>
        level switch
        {
            SlowLevel.Half => ContentFinder<Texture2D>.Get("UI/MSS_FP_SpeedHalf", true),
            SlowLevel.Quarter => ContentFinder<Texture2D>.Get("UI/MSS_FP_SpeedQuarter", true),
            SlowLevel.Eighth => ContentFinder<Texture2D>.Get("UI/MSS_FP_SpeedEighth", true),
            _ => BaseContent.BadTex,
        };

    private static string TooltipFor(SlowLevel level) =>
        level switch
        {
            SlowLevel.Half => "MSSFP_TimeSpeed_Half_Tooltip".Translate(),
            SlowLevel.Quarter => "MSSFP_TimeSpeed_Quarter_Tooltip".Translate(),
            SlowLevel.Eighth => "MSSFP_TimeSpeed_Eighth_Tooltip".Translate(),
            _ => string.Empty,
        };
}
