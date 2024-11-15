using RimWorld;

namespace MSSFP;

public class StorytellerCompProperties_FroggeSpawn : StorytellerCompProperties
{
    public IncidentDef incident;

    public float mtbDays = -1f;

    public StorytellerCompProperties_FroggeSpawn()
    {
        compClass = typeof(StorytellerCompFroggeSpawn);
    }
}
