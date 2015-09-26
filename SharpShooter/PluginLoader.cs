using System;

using LeagueSharp;

namespace SharpShooter
{
    class PluginLoader
    {
        internal static bool LoadPlugin(string PluginName)
        {
            if (CanLoadPlugin(PluginName))
            {
                DynamicInitializer.NewInstance(Type.GetType("SharpShooter.Plugins." + ObjectManager.Player.ChampionName));
                return true;
            }

            return false;
        }

        internal static bool CanLoadPlugin(string PluginName)
        {
            return Type.GetType("SharpShooter.Plugins." + ObjectManager.Player.ChampionName) != null;
        }
    }
}
