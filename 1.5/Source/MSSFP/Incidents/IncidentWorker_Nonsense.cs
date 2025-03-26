using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace MSSFP.Incidents;

public class IncidentWorker_Nonsense : IncidentWorker
{
    public static List<LetterDef> PossibleLetterDefs =
    [
        LetterDefOf.ThreatBig,
        LetterDefOf.ThreatSmall,
        LetterDefOf.NegativeEvent,
        LetterDefOf.NeutralEvent,
        LetterDefOf.PositiveEvent,
        LetterDefOf.PositiveEvent,
        LetterDefOf.PositiveEvent,
        LetterDefOf.PositiveEvent,
    ];

    public override float ChanceFactorNow(IIncidentTarget target) => 1f;

    protected override bool CanFireNowSub(IncidentParms parms) => true;

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Pawn pawn;
        if (parms.target is Map map)
        {
            pawn = map.mapPawns.FreeColonistsSpawned.RandomElement();
        }
        else
        {
            pawn = Find.AnyPlayerHomeMap.mapPawns.FreeColonistsSpawned.RandomElement();
        }

        string letterLabel = ResolveAbsoluteText(pawn, "incidentLetter", true);
        string letterDesc = ResolveAbsoluteText(pawn, "incidentDescription", true);

        float chance = Rand.Value;

        LetterDef letterDef;
        ThoughtDef thoughtDef;

        if (chance < 1 / 3f)
        {
            letterDef = LetterDefOf.NegativeEvent;
            thoughtDef = MSSFPDefOf.MSSFP_Nonsense_Thought_Bad;
        }
        else if (chance < 2 / 3f)
        {
            letterDef = LetterDefOf.NeutralEvent;
            thoughtDef = MSSFPDefOf.MSSFP_Nonsense_Thought_Neutral;
        }
        else
        {
            letterDef = LetterDefOf.PositiveEvent;
            thoughtDef = MSSFPDefOf.MSSFP_Nonsense_Thought_Good;
        }

        SendIncidentLetter(letterLabel, letterDesc, letterDef, parms, new LookTargets([pawn]), def, "");
        TaleRecorder.RecordTale(MSSFPDefOf.MSSFP_Nonsense_Tale, pawn);

        try
        {
            Thought_Memory instance = (Thought_Memory)Activator.CreateInstance(thoughtDef.ThoughtClass);
            instance.def = thoughtDef;
            instance.Init();

            pawn.needs.mood.thoughts.memories.TryGainMemory(instance);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to gain memory for {thoughtDef.defName} on {pawn.NameShortColored} due to {ex}");
        }

        return true;
    }

    public static string ResolveAbsoluteText(Pawn pawn, string absoluteRootKeyword = "root", bool capitalizeFirstSentence = true)
    {
        GrammarRequest req = new GrammarRequest();
        req.Rules.AddRange(MSSFPDefOf.MSS_Nonsense.RulesPlusIncludes);

        foreach (Rule rule in TaleData_Pawn.GenerateFrom(pawn).GetRules("PAWN", req.Constants))
            req.Rules.Add(rule);

        return GrammarResolver.Resolve(absoluteRootKeyword, req, capitalizeFirstSentence: capitalizeFirstSentence);
    }
}
