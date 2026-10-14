using Verse;

namespace MSSFP.Projectiles;

public class ProjectileModExtension : DefModExtension
{
    public IntRange spawnCount = new(1, 1);
    public bool angry;
}
