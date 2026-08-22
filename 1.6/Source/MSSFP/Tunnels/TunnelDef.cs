using Verse;

namespace MSSFP.Tunnels;

public class TunnelDef : Def
{
    [NoTranslate]
    public string worldTransitionGroup = "";
    public float distortionFrequency = 1f;
    public float distortionIntensity;

    public float GetLayerWidth(TunnelWorldLayerDef def)
    {
        return 1;
    }
}
