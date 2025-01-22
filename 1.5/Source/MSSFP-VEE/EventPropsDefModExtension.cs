using Verse;

namespace MSSFP.VEE;

public class EventPropsDefModExtension: DefModExtension
{
    public IntRange GenesToAdd = new IntRange(1, 10);
    public int FalloutCheckTicks = 250;
    public float ChanceForNewGenesEachCheck = 0.1f;
    public float ChanceForRemoveGeneEachCheck = 0.01f;
}
