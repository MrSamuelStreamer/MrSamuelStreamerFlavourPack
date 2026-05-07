using System;
using MSSFP.AICore;
using UnityEngine;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// XML-defined AI personality. Drop a new file under Defs/AICore/Personalities/ to add one.
/// Six shipped: Grep.ai, ChudGPT, Clive, Geminon, Copylot, Deepsink.
/// </summary>
public class AIPersonalityDef : Def
{
    /// <summary>Short label shown in gizmos / messages (e.g. "Grep.ai"). Falls back to <see cref="Def.label"/>.</summary>
    public string labelShort;

    /// <summary>Tint applied to chat messages and letters for this personality.</summary>
    public Color textColor = Color.white;

    /// <summary>Optional gizmo icon path under a Textures folder.</summary>
    public string iconPath;

    /// <summary>Lines used for ambient unsolicited chatter (no target pawn).</summary>
    public RulePackDef ambientChatter;

    /// <summary>Lines used when addressing a specific colonist by name.</summary>
    public RulePackDef pawnAddress;

    /// <summary>Rule pack producing sculpture titles.</summary>
    public RulePackDef artTitles;

    /// <summary>Rule pack producing sculpture descriptions.</summary>
    public RulePackDef artDescriptions;

    /// <summary>Relative weight when a core rolls a personality (random pick mode).</summary>
    public float weight = 1f;

    /// <summary>Concrete worker class that drives this personality. Must derive from <see cref="AIPersonalityWorker"/>.</summary>
    public Type workerClass = typeof(AIPersonalityWorker);

    [Unsaved(false)]
    private AIPersonalityWorker workerInt;

    /// <summary>Lazily-instantiated worker. Stateless — never store per-core state here.</summary>
    public AIPersonalityWorker Worker
    {
        get
        {
            if (workerInt != null)
                return workerInt;
            workerInt = (AIPersonalityWorker)Activator.CreateInstance(workerClass);
            workerInt.def = this;
            return workerInt;
        }
    }

    public string LabelShortOrLabel => string.IsNullOrEmpty(labelShort) ? label : labelShort;
}
