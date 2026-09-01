using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MSSFP.Lockpicking;

/// <summary>
/// No-art timing-bar lockpick minigame. Click when the needle is in the green
/// zone to set tumblers. forcePause is on, so the JobDriver must finish from
/// </summary>
public class Dialog_LockpickMinigame : Window
{
    private readonly Pawn pawn;
    private readonly Building_Door door;
    private readonly Action<bool> onFinished;

    private readonly float zoneWidth;
    private float zoneStart;
    private float needle;
    private int needleDir = 1;
    private int tumblersDone;
    private int triesLeft;
    private float debounce;
    private bool finished;

    public override Vector2 InitialSize => new(520f, 320f);

    public Dialog_LockpickMinigame(Pawn pawn, Building_Door door, Action<bool> onFinished)
    {
        this.pawn = pawn;
        this.door = door;
        this.onFinished = onFinished;

        zoneWidth = LockpickUtility.MinigameZoneWidth(pawn);
        LockpickUtility.RandomizeZone(zoneWidth, out zoneStart);
        triesLeft = LockpickUtility.MinigameTries;

        doCloseX = true;
        absorbInputAroundWindow = true;
        forcePause = true;
        closeOnClickedOutside = false;
        closeOnAccept = false;
        closeOnCancel = true;
    }

    public override void PreClose()
    {
        if (finished)
            return;
        finished = true;
        onFinished?.Invoke(false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (finished)
            return;

        float dt = Time.unscaledDeltaTime;
        if (debounce > 0f)
            debounce -= dt;

        float speed = LockpickUtility.MinigameNeedleSpeedFor(tumblersDone);
        needle += needleDir * speed * dt;
        if (needle >= 1f)
        {
            needle = 1f;
            needleDir = -1;
        }
        else if (needle <= 0f)
        {
            needle = 0f;
            needleDir = 1;
        }

        float curY = inRect.y;

        Text.Font = GameFont.Medium;
        Widgets.Label(
            new Rect(inRect.x, curY, inRect.width, 32f),
            "MSSFP_LockpickMinigame_Title".Translate(pawn.LabelShort, door.LabelShort)
        );
        Text.Font = GameFont.Small;
        curY += 34f;

        Widgets.Label(
            new Rect(inRect.x, curY, inRect.width, 22f),
            "MSSFP_LockpickMinigame_Hint".Translate()
        );
        curY += 26f;

        Widgets.Label(
            new Rect(inRect.x, curY, inRect.width, 22f),
            "MSSFP_LockpickMinigame_Tumblers".Translate(
                tumblersDone,
                LockpickUtility.MinigameTumblers
            )
        );
        curY += 24f;

        DrawSquares(
            new Rect(inRect.x, curY, inRect.width, 18f),
            LockpickUtility.MinigameTumblers,
            tumblersDone,
            ColorLibrary.Green
        );
        curY += 26f;

        Rect track = new(inRect.x, curY, inRect.width, 40f);
        DrawTrack(track);
        curY += 50f;

        Widgets.Label(
            new Rect(inRect.x, curY, inRect.width, 22f),
            "MSSFP_LockpickMinigame_Tries".Translate(triesLeft)
        );
        curY += 24f;

        DrawSquares(
            new Rect(inRect.x, curY, inRect.width, 14f),
            LockpickUtility.MinigameTries,
            triesLeft,
            ColorLibrary.Yellow
        );

        Rect cancelRect = new(inRect.x, inRect.yMax - 40f, 160f, 36f);
        if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
        {
            Finish(false);
            return;
        }

        bool space =
            Event.current.type == EventType.KeyDown
            && (
                Event.current.keyCode == KeyCode.Space
                || Event.current.keyCode == KeyCode.Return
                || Event.current.keyCode == KeyCode.KeypadEnter
            );
        bool mouse =
            Event.current.type == EventType.MouseDown
            && Event.current.button == 0
            && Mouse.IsOver(inRect)
            && !Mouse.IsOver(cancelRect);

        if ((space || mouse) && debounce <= 0f)
        {
            Event.current.Use();
            TryClick();
        }
    }

    private void DrawTrack(Rect track)
    {
        Widgets.DrawBoxSolid(track, new Color(0.15f, 0.15f, 0.15f));
        Widgets.DrawBox(track);

        Rect zone = new(
            track.x + zoneStart * track.width,
            track.y,
            Mathf.Max(4f, zoneWidth * track.width),
            track.height
        );
        Widgets.DrawBoxSolid(zone, ColorLibrary.Green);

        float needleX = track.x + needle * track.width;
        Rect needleRect = new(needleX - 2f, track.y, 4f, track.height);
        Widgets.DrawBoxSolid(needleRect, ColorLibrary.Yellow);
    }

    private static void DrawSquares(Rect row, int total, int filled, Color fill)
    {
        const float size = 16f;
        const float gap = 6f;
        for (int i = 0; i < total; i++)
        {
            Rect sq = new(row.x + i * (size + gap), row.y, size, size);
            if (i < filled)
                Widgets.DrawBoxSolid(sq, fill);
            else
                Widgets.DrawBoxSolid(sq, new Color(0.2f, 0.2f, 0.2f));
            Widgets.DrawBox(sq);
        }
    }

    private void TryClick()
    {
        debounce = LockpickUtility.MinigameClickDebounce;
        bool hit = needle >= zoneStart && needle <= zoneStart + zoneWidth;
        if (hit)
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            tumblersDone++;
            if (tumblersDone >= LockpickUtility.MinigameTumblers)
            {
                Finish(true);
                return;
            }

            LockpickUtility.RandomizeZone(zoneWidth, out zoneStart);
            needle = 0f;
            needleDir = 1;
        }
        else
        {
            SoundDefOf.ClickReject.PlayOneShotOnCamera();
            triesLeft--;
            if (triesLeft <= 0)
                Finish(false);
        }
    }

    private void Finish(bool success)
    {
        if (finished)
            return;
        finished = true;
        onFinished?.Invoke(success);
        Close();
    }
}
