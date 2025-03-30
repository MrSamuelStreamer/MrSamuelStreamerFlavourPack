using UnityEngine;
using Verse;

namespace MSSFP;

[StaticConstructorOnStartup]
public static class Textures
{
    public static Texture2D MSSFP_OskarianBed_Upgrade = ContentFinder<Texture2D>.Get("UI/MSSFP_OskarianBed_Upgrade");
}
