using UnityEngine;
using Verse;

namespace MSSFP.AICore;

/// <summary>
/// Single speech-bubble instance over an AI-core building.
///
/// INVARIANTS:
/// - Pure render-time state. NOT scribed.
/// - Created by <see cref="AICoreBubbler.Add"/> from sim-thread code (CompTrueAICore chatter).
/// - <see cref="Draw"/> called by <see cref="AICoreBubbler.OnGUI"/> on the Unity main thread —
///   RimWorld is single-threaded so sim and OnGUI share that thread; no race.
/// - <see cref="Draw"/> returns false when fully faded → bubbler evicts the instance.
///
/// </summary>
public class AICoreBubble
{
    /// <summary>Bubble text. Already resolved by personality Worker.</summary>
    public readonly string text;

    /// <summary>Foreground tint = personality.textColor.</summary>
    public readonly Color color;

    /// <summary>Game tick when posted. Mutable so FIFO eviction can call <see cref="ForceFade"/>.</summary>
    public int tickPosted;

    /// <summary>Computed each Draw frame. Pixel size at current camera scale.</summary>
    public int Width;
    public int Height;

    private GUIStyle style;

    /// <summary>Tick when fade begins. ~10s game.</summary>
    public const int FadeStartTicks = 600;

    /// <summary>Ticks of fade-out before drop. ~5s game.</summary>
    public const int FadeLengthTicks = 300;

    /// <summary>Initial opacity at post-time.</summary>
    public const float OpacityStart = 0.95f;

    /// <summary>Background fill opacity multiplier (multiplied by current fade).</summary>
    public const float BackgroundOpacity = 0.92f;

    /// <summary>Max bubble width in px (at scale=1).</summary>
    public const int WidthMax = 220;

    /// <summary>Padding inside bubble (at scale=1).</summary>
    public const int PaddingX = 6;
    public const int PaddingY = 4;

    /// <summary>Base font size at scale=1.</summary>
    public const int FontSize = 12;

    public AICoreBubble(string text, Color color, int tickPosted)
    {
        this.text = text ?? string.Empty;
        this.color = color;
        this.tickPosted = tickPosted;
    }

    private GUIStyle Style
    {
        get
        {
            if (style == null)
            {
                style = new GUIStyle(Text.CurFontStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    wordWrap = true,
                };
            }
            return style;
        }
    }

    /// <summary>
    /// Render the bubble. <paramref name="anchor"/> is the per-host label-pos above the building.
    /// <paramref name="verticalOffsetPx"/> stacks bubbles upward (caller increments per stacked bubble).
    /// Returns true to keep, false to evict.
    /// </summary>
    public bool Draw(Vector2 anchor, float scale, int verticalOffsetPx)
    {
        float fade = GetFade();
        if (fade <= 0f) return false;

        // Recompute font + padding + dims each frame; cheap and survives style cache miss.
        Style.fontSize = Mathf.Max(8, Mathf.RoundToInt(FontSize * scale));
        int padX = Mathf.Max(2, Mathf.RoundToInt(PaddingX * scale));
        int padY = Mathf.Max(2, Mathf.RoundToInt(PaddingY * scale));
        Style.padding = new RectOffset(padX, padX, padY, padY);

        GUIContent content = new GUIContent(text);
        Width = Mathf.RoundToInt(Mathf.Min(Style.CalcSize(content).x, WidthMax * scale));
        Height = Mathf.RoundToInt(Style.CalcHeight(content, Width));

        Rect rect = new Rect(
            Mathf.Ceil(anchor.x - Width * 0.5f),
            Mathf.Ceil(anchor.y - verticalOffsetPx - Height),
            Width,
            Height);

        // Click-to-dismiss: any MouseDown landing inside the rect accelerates fade and consumes
        // the click. Trade-off: a click on a bubble overlapping its host eats the host-select click
        // for that frame; player must click again to select the underlying building. Acceptable —
        // bubbles are short-lived and the alternative is a no-dismiss UI.
        Event ev = Event.current;
        if (ev != null && ev.type == EventType.MouseDown && rect.Contains(ev.mousePosition))
        {
            ForceFade();
            ev.Use();
            // Repaint pass still draws this frame — keep the bubble for one more draw to avoid flicker.
            // Next frame fade will be ~0 and Draw returns false.
        }

        Color prev = GUI.color;

        // Background: dark near-opaque fill (no PNG atlas in v1).
        Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, fade * BackgroundOpacity));

        // Outline: personality color, faded.
        GUI.color = new Color(color.r, color.g, color.b, fade);
        Widgets.DrawBox(rect, 1);

        // Text: personality color, faded.
        GUI.color = new Color(color.r, color.g, color.b, fade);
        GUI.Label(rect, text, Style);

        GUI.color = prev;
        return true;
    }

    /// <summary>
    /// Fade curve: full opacity until FadeStartTicks elapsed, then linear to 0 over FadeLengthTicks.
    /// </summary>
    public float GetFade()
    {
        int age = Find.TickManager.TicksGame - tickPosted;
        if (age <= FadeStartTicks) return OpacityStart;
        int fadeAge = age - FadeStartTicks;
        if (fadeAge >= FadeLengthTicks) return 0f;
        return OpacityStart * (1f - (float)fadeAge / FadeLengthTicks);
    }

    /// <summary>Tail-end ticks left after a forced fade. ~0.5s game.</summary>
    public const int ForceFadeTailTicks = 30;

    /// <summary>
    /// Eviction: collapse the fade phase to <see cref="ForceFadeTailTicks"/> remaining ticks. Used
    /// for both FIFO overflow (visual pop avoidance) and explicit click-to-dismiss. Idempotent —
    /// later calls won't extend the lifetime if already past the threshold.
    /// </summary>
    public void ForceFade()
    {
        if (Find.TickManager == null) return;
        int now = Find.TickManager.TicksGame;
        int target = now - (FadeStartTicks + FadeLengthTicks - ForceFadeTailTicks);
        // Only move tickPosted backward (i.e. shorten lifetime). Never extend.
        if (target < tickPosted)
            tickPosted = target;
    }
}
