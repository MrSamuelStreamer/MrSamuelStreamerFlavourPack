﻿using Verse;

namespace MSSFP;

public class LoversRetreatGameomponent(Game game) : GameComponent
{

    public bool _loversRetreatEnabled = true;

    public bool LoversRetreatEnabled
    {
        get
        {
            return MSSFPMod.settings.EnableLoversRetreat && _loversRetreatEnabled;
        }
        set
        {
            _loversRetreatEnabled = value;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _loversRetreatEnabled, "LoversRetreatEnabled", true);
    }
}
