using Verse;

namespace MSSFP.ModExtensions;

public class AutoSpawningModExtension: DefModExtension
{
    public int notBeforeTick = 0;
    public IntRange randomFuzzRange = new(0, 0);
    public LetterDef spawnedLetterType;
    public string spawnedLetterLabel;
    public string spawnedLetterText;
}
