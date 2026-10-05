using System;
using UnityEngine;
using Verse;

namespace MSSFP.LabelIcons;

/// <summary>
/// One icon that can appear beside a pawn's map label. Live icons are checked
/// every frame (cheap flags such as Drafted); the rest are cached briefly
/// because they scan hediffs.
/// </summary>
public sealed class LabelIcon
{
    public readonly string Id;
    public readonly Texture2D Texture;
    public readonly Color Tint;
    public readonly bool Pulse;
    public readonly bool Live;
    public readonly Func<Pawn, bool> Applies;

    public LabelIcon(string id, string texturePath, Color tint, Func<Pawn, bool> applies,
        bool live = false, bool pulse = false)
    {
        Id = id;
        Texture = ContentFinder<Texture2D>.Get(texturePath);
        Tint = tint;
        Applies = applies;
        Live = live;
        Pulse = pulse;
    }

    public string LabelKey => "MSS_FP_Settings_LabelIcon_" + Id;
}
